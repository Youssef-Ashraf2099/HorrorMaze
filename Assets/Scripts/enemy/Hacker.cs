using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Required for UI elements like Canvas and Text
using UnityEngine.Video; // Required for the VideoPlayer

public class Hacker : Enemy
{
    [Header("Hacker Specifics")]
    [Tooltip("How long the player must look at the Hacker to trigger the effect.")]
    public float lookAtDuration = 2.0f;

    [Tooltip("The maximum distance from which the player's look can be detected.")]
    public float maxLookAtDistance = 15.0f;

    [Tooltip("The angle from the center of the player's view the Hacker must be within.")]
    public float lookAtAngle = 10.0f;

    [Tooltip("The angle defining the 'front' of the Hacker for look detection.")]
    public float hackerFov = 90.0f; // Player must be within this cone in front of the Hacker

    [Tooltip("A specific point on the Hacker to target for look detection (e.g., the head or chest). If empty, the pivot point is used.")]
    public Transform lookAtTarget;

    [Header("State Animations")]
    public AnimationClip idleAnimation;
    public AnimationClip patrolAnimation; // Animation for patrolling
    public AnimationClip attackAnimation; // Animation for the jumpscare/prank sequence

    [Header("Jumpscare Video")]
    [Tooltip("The Quad or other object that will display the jumpscare video.")]
    public GameObject jumpscareScreen;
    [Tooltip("The VideoPlayer component that plays the jumpscare clip.")]
    public VideoPlayer jumpscareVideoPlayer;


    [Header("Prank Components")]
    [Tooltip("The Canvas object containing the fake file deletion UI.")]
    public GameObject prankCanvas;

    [Tooltip("The UI Text element to display the fake file deletion progress.")]
    public Text prankText;

    [Tooltip("Sound to play during the fake deletion sequence.")]
    public AudioClip prankSound;

    private float lookTimer = 0f;
    private bool isSequenceActive = false;
    private AudioSource prankAudioSource;
    private int currentPatrolIndex = 0;

    protected override void Start()
    {
        base.Start();
        // Allow the Hacker to move for patrolling.
        if (agent != null)
        {
            agent.isStopped = false;
            // Set a stopping distance to prevent the Hacker from getting too close to the player.
            // This ensures the look-at raycast can function correctly.
            agent.stoppingDistance = this.attackRange;
        }
        // Set initial state to Patrolling if points are assigned.
        currentState = (patrolPoints != null && patrolPoints.Length > 0) ? EnemyState.Patrolling : EnemyState.Idle;

        // --- CRITICAL CHECK: Ensure there is a collider ---
        Collider col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (col == null)
        {
            Debug.LogError("Hacker Error: This enemy has no Collider component. The player's look detection (Raycast) will not work. Please add a Collider.", this.gameObject);
        }
        else if (col.isTrigger)
        {
            Debug.LogError("Hacker Error: The Collider on this enemy is set to 'Is Trigger'. The Raycast will not detect it. Please uncheck 'Is Trigger' on the Collider component.", this.gameObject);
        }

        // Ensure video components are ready
        if (jumpscareScreen != null) jumpscareScreen.SetActive(false);
        if (jumpscareVideoPlayer != null) jumpscareVideoPlayer.Prepare();


        // Ensure prank components are set up correctly
        if (prankCanvas != null)
        {
            prankCanvas.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Hacker: Prank Canvas has not been assigned.");
        }

        // Configure a dedicated AudioSource for the prank sound
        prankAudioSource = gameObject.AddComponent<AudioSource>();
        prankAudioSource.playOnAwake = false;
        prankAudioSource.loop = true;
        if (prankSound != null)
        {
            prankAudioSource.clip = prankSound;
        }
    }

    protected override void Update()
    {
        if (isSequenceActive || player == null)
        {
            return;
        }

        // Always check if the player is looking at the Hacker's front.
        HandleLookDetection();

        // Check player proximity to decide behavior.
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < aggroRange)
        {
            currentState = EnemyState.Chasing;
            ChaseBehavior();
        }
        else
        {
            // If we were chasing, switch back to patrolling.
            if (currentState == EnemyState.Chasing)
            {
                currentState = EnemyState.Patrolling;
            }

            if (currentState == EnemyState.Patrolling)
            {
                PatrolBehavior();
            }
        }

        // Update the animator based on the current state and agent velocity.
        UpdateAnimator();
    }

    /// <summary>
    /// Draws a visual representation of the look-at detection cone in the editor.
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // Draw the base gizmos (like aggro range)

        // Draw the Hacker's own FOV for the jumpscare trigger
        Gizmos.color = Color.cyan;
        Vector3 fovLine1 = Quaternion.AngleAxis(hackerFov * 0.5f, transform.up) * transform.forward * maxLookAtDistance;
        Vector3 fovLine2 = Quaternion.AngleAxis(-hackerFov * 0.5f, transform.up) * transform.forward * maxLookAtDistance;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);
    }

    /// <summary>
    /// A more robust method to check if the player is looking at the Hacker.
    /// </summary>
    private void HandleLookDetection()
    {
        if (mainCamera == null || player == null) return;

        // Use the specific look-at target if available, otherwise default to the transform's position + an offset.
        Vector3 targetPoint = lookAtTarget != null ? lookAtTarget.position : transform.position + Vector3.up * 1.5f;
        float distanceToPlayer = Vector3.Distance(mainCamera.transform.position, transform.position);

        // --- Condition 1: Is the player looking towards the Hacker? ---
        Vector3 directionToTarget = (targetPoint - mainCamera.transform.position).normalized;
        float playerLookAngle = Vector3.Angle(mainCamera.transform.forward, directionToTarget);

        // --- Condition 2: Is the Hacker generally facing the player? ---
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float hackerFacingAngle = Vector3.Angle(transform.forward, directionToPlayer);

        // Check if the player is within the look-at cone and distance constraints.
        if (playerLookAngle < lookAtAngle && hackerFacingAngle < hackerFov * 0.5f && distanceToPlayer < maxLookAtDistance)
        {
            bool canSee = false;

            // At very close range, the raycast can fail. We assume clear sight if the player is this close.
            if (distanceToPlayer < 1.5f)
            {
                canSee = true;
            }
            else
            {
                // Use a Linecast to check for obstructions between the camera and the target point.
                // This is more reliable than a standard Raycast from inside a collider.
                if (Physics.Linecast(mainCamera.transform.position, targetPoint, out RaycastHit hit))
                {
                    // We have a clear line of sight if the object hit is part of the Hacker.
                    if (hit.transform.IsChildOf(this.transform) || hit.transform == this.transform)
                    {
                        canSee = true;
                    }
                }
                else
                {
                    // No object was hit, meaning there is a clear line of sight.
                    canSee = true;
                }
            }

            if (canSee)
            {
                lookTimer += Time.deltaTime;
                if (lookTimer >= lookAtDuration)
                {
                    OnPlayerCaught();
                }
                return; // Exit to prevent the timer from resetting.
            }
        }

        // If any condition fails, reset the timer.
        lookTimer = 0f;
    }

    /// <summary>
    /// The Hacker's main attack: a jumpscare followed by a system prank.
    /// </summary>
    protected override void OnPlayerCaught()
    {
        if (isSequenceActive) return;
        isSequenceActive = true; // Set flag immediately to prevent re-triggering

        if (playerSanity != null)
        {
            playerSanity.PlayerCaught();
        }

        StartCoroutine(JumpscareAndPrankSequence());
    }

    /// <summary>
    /// Overrides the base jumpscare to play a video.
    /// </summary>
    public override void TriggerJumpscare()
    {
        if (playerMovement != null) playerMovement.SetInputActive(false);

        SwitchToJumpscareCamera();

        if (modelRenderer != null) modelRenderer.enabled = false;

        if (jumpscareScreen != null) jumpscareScreen.SetActive(true);
        if (jumpscareVideoPlayer != null) jumpscareVideoPlayer.Play();

        if (jumpscareSound != null) AudioSource.PlayClipAtPoint(jumpscareSound, transform.position);
        if (animator != null && !string.IsNullOrEmpty(jumpscareAnimationTrigger))
        {
            animator.SetTrigger(jumpscareAnimationTrigger);
        }
    }

    private IEnumerator JumpscareAndPrankSequence()
    {
        lookTimer = 0f;
        currentState = EnemyState.Attacking;
        if (agent != null) agent.isStopped = true; // Stop moving during the attack sequence

        // --- Part 1: Jumpscare with Video ---
        TriggerJumpscare();

        // Wait for the duration of the video clip to finish
        float videoDuration = (jumpscareVideoPlayer != null && jumpscareVideoPlayer.clip != null) ? (float)jumpscareVideoPlayer.clip.length : 2.0f;
        yield return new WaitForSeconds(videoDuration);

        // --- Part 2: The Prank ---
        // Clean up the jumpscare video
        if (jumpscareVideoPlayer != null) jumpscareVideoPlayer.Stop();
        if (jumpscareScreen != null) jumpscareScreen.SetActive(false);
        SwitchToMainCameraNoInput();

        // Activate the prank UI
        if (prankCanvas != null) prankCanvas.SetActive(true);
        if (prankAudioSource != null) prankAudioSource.Play();

        yield return StartCoroutine(SimulateFileDeletion());

        // --- Part 3: Cleanup and Exit ---
        if (prankAudioSource != null) prankAudioSource.Stop();

        //if (prankText != null)
        //{
        //    prankText.text = "Get pranked.";
        //}
        yield return new WaitForSeconds(3f);

        Debug.Log("Hacker prank finished. Quitting application.");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator SimulateFileDeletion()
    {
        if (prankText == null) yield break;

        string[] fakeFiles = {
            "C:\\Windows\\System32\\kernel32.dll", "C:\\Windows\\System32\\user32.dll",
            "C:\\Windows\\System32\\gdi32.dll", "C:\\Windows\\System32\\ntoskrnl.exe",
            "C:\\Users\\You\\Documents\\secrets.txt", "C:\\Program Files\\YourFavoriteGame\\save_data.sav",
            "INITIATING KERNEL PANIC...", "DELETING SYSTEM REGISTRY...",
            "PURGING SHADOW COPIES...", "FORMATTING C: DRIVE..."
        };

        prankText.text = "SYSTEM INTEGRITY COMPROMISED. ACCESSING KERNEL...";
        yield return new WaitForSeconds(2.5f);

        foreach (string file in fakeFiles)
        {
            prankText.text = $"DELETING: {file}...";
            yield return new WaitForSeconds(Random.Range(0.7f, 1.3f));
        }

        prankText.text = "FATAL SYSTEM ERROR. RESTART YOUR UNIVERSE.";
        yield return new WaitForSeconds(3f);
    }

    /// <summary>
    /// A custom version of SwitchToMainCamera that does not re-enable player input.
    /// </summary>
    private void SwitchToMainCameraNoInput()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        if (jumpscareCamera != null) jumpscareCamera.gameObject.SetActive(false);
    }

    /// <summary>
    /// Overrides the base animator logic to play specific animation clips.
    /// </summary>
    protected override void UpdateAnimator()
    {
        if (animator == null || agent == null) return;

        // Use agent velocity to drive the "Speed" parameter for walk/idle transitions.
        float normalizedSpeed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("Speed", normalizedSpeed);

        // Also handle specific state animations like the attack.
        if (currentState == EnemyState.Attacking)
        {
            if (attackAnimation != null && !animator.GetCurrentAnimatorStateInfo(0).IsName(attackAnimation.name))
            {
                animator.Play(attackAnimation.name);
            }
        }
    }

    // The Hacker does not use these behaviors, so they are overridden to do nothing.
    protected override void IdleBehavior() { }

    protected override void PatrolBehavior()
    {
        if (agent != null) agent.updateRotation = true; // Let the agent control rotation while patrolling

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentState = EnemyState.Idle;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    /// <summary>
    /// When the player is close, the Hacker will look at and move towards them.
    /// </summary>
    protected override void ChaseBehavior()
    {
        if (player == null || agent == null) return;

        // Move towards the player, but stop at the attackRange.
        agent.SetDestination(player.position);

        // When stalking, the Hacker should be very responsive in looking at the player.
        // We take manual control of rotation from the NavMeshAgent for a better feel.
        agent.updateRotation = false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            // Use a dedicated rotation speed to make it feel more alert. A higher value is snappier.
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    protected override void AttackBehavior() { }
}