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

    [Header("State Animations")]
    public AnimationClip idleAnimation;
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

    protected override void Start()
    {
        base.Start();
        // The Hacker should not move, so we keep its agent stopped.
        if (agent != null)
        {
            agent.isStopped = true;
        }
        currentState = EnemyState.Idle;

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
        // This override prevents the base Enemy.Update() from running.
        // The Hacker's logic is entirely self-contained.
        if (isSequenceActive)
        {
            return;
        }

        // The Hacker's only "behavior" is to wait for the player to look at it.
        HandleLookDetection();

        // We still call UpdateAnimator to ensure the idle animation plays correctly.
        UpdateAnimator();
    }

    /// <summary>
    /// Draws a visual representation of the look-at detection cone in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (mainCamera != null)
        {
            Gizmos.color = Color.yellow;
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = mainCamera.transform.localToWorldMatrix;

            // Draw the detection cone from the camera
            Gizmos.DrawFrustum(Vector3.zero, lookAtAngle * 2, maxLookAtDistance, 0.1f, 1f);

            Gizmos.matrix = originalMatrix;
        }
    }

    /// <summary>
    /// Checks if the player is looking directly at the Hacker.
    /// </summary>
    private void HandleLookDetection()
    {
        // Ensure we have a reference to the main camera.
        if (mainCamera == null || player == null) return;

        Vector3 directionToEnemy = (transform.position - mainCamera.transform.position).normalized;
        float angle = Vector3.Angle(mainCamera.transform.forward, directionToEnemy);

        // --- DEBUG: Draw the ray in the scene view ---
        Debug.DrawRay(mainCamera.transform.position, directionToEnemy * maxLookAtDistance, Color.red);

        if (angle < lookAtAngle && Vector3.Distance(mainCamera.transform.position, transform.position) < maxLookAtDistance)
        {
            if (Physics.Raycast(mainCamera.transform.position, directionToEnemy, out RaycastHit hit, maxLookAtDistance))
            {
                if (hit.transform == this.transform)
                {
                    lookTimer += Time.deltaTime;
                    if (lookTimer >= lookAtDuration)
                    {
                        OnPlayerCaught();
                    }
                    return;
                }
                else
                {
                    // --- DEBUG: Log what the ray hit if it wasn't the hacker ---
                    //Debug.Log($"Hacker look-at raycast hit '{hit.transform.name}' instead of the Hacker.");
                }
            }
        }
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
        if (animator == null) return;

        AnimationClip clipToPlay = null;
        switch (currentState)
        {
            case EnemyState.Idle:
                clipToPlay = idleAnimation;
                break;
            case EnemyState.Attacking:
                clipToPlay = attackAnimation;
                break;
        }

        if (clipToPlay != null)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(clipToPlay.name))
            {
                animator.Play(clipToPlay.name);
            }
        }
    }

    // The Hacker does not use these behaviors, so they are overridden to do nothing.
    // This prevents the base class from making the Hacker move.
    protected override void IdleBehavior() { }
    protected override void PatrolBehavior() { }
    protected override void ChaseBehavior() { }
    protected override void AttackBehavior() { }
}