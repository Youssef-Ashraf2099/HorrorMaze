using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum EnemyState
{
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Stunned
}

[System.Serializable]
public class EnemyAudioClips
{
    public AudioClip Footstep;
    public AudioClip Idle;
    public AudioClip ChaseAlert;
}

public abstract class Enemy : MonoBehaviour
{
    // Static list to keep track of all enemy instances
    public static readonly List<Enemy> AllEnemies = new List<Enemy>();

    [Header("Enemy Attributes")]
    public float speed = 3.0f;
    public float visionDistance = 10.0f;
    public float fieldOfView = 120.0f;
    public float aggroRange = 8.0f;
    public float attackRange = 2.0f;
    public float attackCooldown = 2.0f;
    public EnemyAudioClips enemySounds;
    public AudioClip jumpscareSound; // Dedicated jumpscare sound
    public GameObject jumpscareObject; // Jumpscare object (3D model, canvas, etc.)
    public string jumpscareAnimationTrigger = "Jumpscare"; // Animator trigger for jumpscare
    public Animator animator;
    public Transform[] patrolPoints;
    public Camera mainCamera;
    public Camera jumpscareCamera;
    public float jumpscareDuration = 2.5f;

    [Header("Audio Settings")]
    [Tooltip("Time between footstep sounds while moving.")]
    public float footstepInterval = 0.5f;
    [Tooltip("Minimum time between idle sounds.")]
    public float minIdleSoundTime = 5.0f;
    [Tooltip("Maximum time between idle sounds.")]
    public float maxIdleSoundTime = 10.0f;
    [Tooltip("The distance at which the sound starts to fade.")]
    public float audioMinDistance = 3.0f;
    [Tooltip("The distance at which the sound is no longer audible.")]
    public float audioMaxDistance = 50.0f;


    [Header("State")]
    public EnemyState currentState = EnemyState.Idle;
    protected float lastAttackTime;

    protected NavMeshAgent agent;
    protected AudioSource audioSource; // Renamed for clarity
    private int currentPatrolIndex = 0;
    protected Transform player;
    protected playerMovment playerMovement; // Add this line
    protected Sanity playerSanity; // Reference to the player's sanity script
    protected Renderer modelRenderer;

    protected Vector3 initialPosition;
    protected Quaternion initialRotation;
    private float originalSpeed;
    private Coroutine speedBoostCoroutine;
    private Coroutine persistentChaseCoroutine;
    private float footstepTimer;
    private float idleSoundTimer;
    protected bool isJumpscaring = false;

    protected virtual void OnEnable()
    {
        // Add this enemy to the static list when it becomes active
        if (!AllEnemies.Contains(this))
        {
            AllEnemies.Add(this);
        }
    }

    protected virtual void OnDisable()
    {
        // Remove this enemy from the list when it becomes inactive or is destroyed
        if (AllEnemies.Contains(this))
        {
            AllEnemies.Remove(this);
        }
    }


    protected virtual void OnDrawGizmosSelected()
    {
        // Draw the vision cone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionDistance);

        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfView * 0.5f, transform.up) * transform.forward * visionDistance;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfView * 0.5f, transform.up) * transform.forward * visionDistance;
        Gizmos.DrawLine(transform.position, transform.position + fovLine1);
        Gizmos.DrawLine(transform.position, transform.position + fovLine2);

        // Draw the aggro range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        // Draw the attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw Audio Ranges
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, audioMinDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, audioMaxDistance);
    }
    protected virtual void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        agent = GetComponent<NavMeshAgent>(); // Add this line

        // Setup Audio Source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource for 3D sound
        ConfigureAudioSource(audioSource);


        originalSpeed = speed;
        agent.speed = speed;                  // Also set the agent's speed
        modelRenderer = GetComponentInChildren<Renderer>();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // --- THEN, get the movement component from the player ---
        if (player != null)
        {
            playerMovement = player.GetComponent<playerMovment>();
            playerSanity = player.GetComponent<Sanity>(); // Get the Sanity component
        }
        if (player == null)
        {
            Debug.LogWarning($"{name}: Player not found in the scene. Enemy will not function properly.");
        }
        if (animator == null)
        {
            Debug.LogWarning($"{name}: Animator not assigned. Animations will not play.");
        }
        if (enemySounds == null)
        {
            Debug.LogWarning($"{name}: Enemy sounds not assigned. No sounds will play.");
        }
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: Patrol points not assigned. Patrolling will not work.");
        }
        if (mainCamera == null)
        {
            Debug.LogWarning($"{name}: Main camera not assigned.");
        }
        if (jumpscareCamera == null)
        {
            Debug.LogWarning($"{name}: Jumpscare camera not assigned.");
        }
        if (jumpscareSound == null)
        {
            Debug.LogWarning($"{name}: Jumpscare sound not assigned.");
        }
        if (jumpscareObject == null)
        {
            Debug.LogWarning($"{name}: Jumpscare object not assigned.");
        }
        else
        {
            jumpscareObject.SetActive(false);
        }
        if (modelRenderer == null)
        {
            Debug.LogWarning($"{name}: Enemy model renderer not found. Cannot hide/show model during jumpscare.");
        }
        ResetIdleSoundTimer();
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.spatialBlend = 1.0f; // Set to 3D
        source.rolloffMode = AudioRolloffMode.Linear; // Common falloff mode
        source.minDistance = audioMinDistance;
        source.maxDistance = audioMaxDistance;
    }

    protected virtual void Update()
    {
        // Halt all state machine logic and sound handling if a jumpscare is active.
        if (player == null || currentState == EnemyState.Stunned || persistentChaseCoroutine != null || isJumpscaring)
        {
            UpdateAnimator(); // Update animator even when stunned to show idle/stun animation
            return;
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                IdleBehavior();
                break;  
            case EnemyState.Patrolling:
                PatrolBehavior();
                break;
            case EnemyState.Chasing:
                ChaseBehavior();
                break;
            case EnemyState.Attacking:
                AttackBehavior();
                break;
        }

        UpdateAnimator();
        HandleSounds();
    }

    /// <summary>
    /// Updates the animator based on the current state of the enemy.
    /// This method can be overridden by subclasses for custom animation logic.
    /// </summary>
    protected virtual void UpdateAnimator()
    {
        // Do nothing if no animator is assigned.
        if (animator == null) return;

        // Use the agent's velocity to drive a "Speed" parameter in the animator.
        // This is a common pattern for controlling walk/run/idle animations.
        float normalizedSpeed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("Speed", normalizedSpeed);

        // You can also pass the current state to the animator if you have specific
        // animations for states like Attacking or Stunned.
        animator.SetInteger("State", (int)currentState);
    }

    protected abstract void OnPlayerCaught();

    protected virtual void Respawn()
    {
        // Use agent.Warp() to instantly teleport the NavMeshAgent
        if (agent != null)
        {
            agent.Warp(initialPosition);
        }
        transform.rotation = initialRotation;

        // Reset the enemy's state to start its cycle over
        TransitionToState(EnemyState.Idle);
    }
    protected virtual void IdleBehavior()
    {
        if (CanSeePlayer() || Vector3.Distance(transform.position, player.position) < aggroRange)
        {
            TransitionToState(EnemyState.Chasing);
        }
        else if (patrolPoints != null && patrolPoints.Length > 0)
        {
            TransitionToState(EnemyState.Patrolling);
        }
    }

    protected virtual void PatrolBehavior()
    {
        if (CanSeePlayer() || Vector3.Distance(transform.position, player.position) < aggroRange)
        {
            TransitionToState(EnemyState.Chasing);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0 || !agent.hasPath || agent.remainingDistance < 0.5f)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
            else
            {
                TransitionToState(EnemyState.Idle);
            }
        }
    }

    protected virtual void ChaseBehavior()
    {
        if (!CanSeePlayer() && Vector3.Distance(transform.position, player.position) > aggroRange)
        {
            TransitionToState((patrolPoints != null && patrolPoints.Length > 0) ? EnemyState.Patrolling : EnemyState.Idle);
            return;
        }

        agent.SetDestination(player.position);

        // Only transition to attacking if in range AND the attack is off cooldown.
        if (Vector3.Distance(transform.position, player.position) < attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            TransitionToState(EnemyState.Attacking);
        }
    }

    protected virtual void AttackBehavior()
    {
        agent.ResetPath();
        transform.LookAt(player);

        // Set the last attack time and trigger the OnPlayerCaught sequence.
        // The state will be managed by the jumpscare logic from this point.
        lastAttackTime = Time.time;
        OnPlayerCaught();
    }

    protected virtual void StunnedBehavior() { /* Stun logic here */ }

    protected bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 directionToPlayer = player.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (directionToPlayer.magnitude < visionDistance && angle < fieldOfView * 0.5f)
        {
            if (Physics.Raycast(transform.position, directionToPlayer.normalized, out RaycastHit hit, visionDistance))
            {
                return hit.transform == player;
            }
        }
        return false;
    }

    protected void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void HandleSounds()
    {
        bool isMoving = agent.velocity.magnitude > 0.1f && (currentState == EnemyState.Patrolling || currentState == EnemyState.Chasing);

        if (isMoving)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                PlaySfx(enemySounds.Footstep); // Play footstep sound
                footstepTimer = footstepInterval;
            }
        }

        if (currentState == EnemyState.Idle)
        {
            idleSoundTimer -= Time.deltaTime;
            if (idleSoundTimer <= 0)
            {
                PlaySfx(enemySounds.Idle); // Play idle sound
                ResetIdleSoundTimer();
            }
        }
    }

    private void ResetIdleSoundTimer()
    {
        idleSoundTimer = Random.Range(minIdleSoundTime, maxIdleSoundTime);
    }

    protected void TransitionToState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Play a sound when transitioning to the chase state
        if (newState == EnemyState.Chasing && currentState != EnemyState.Chasing)
        {
            PlaySfx(enemySounds.ChaseAlert); // Play chase alert sound
        }

        currentState = newState;
    }

    public virtual void TriggerJumpscare()
    {
        isJumpscaring = true;

        // Stop all movement and sounds immediately
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (playerMovement != null) playerMovement.SetInputActive(false);// Freeze player

        // Stop the heartbeat effect on the player
        HeartBeat playerHeartbeat = player?.GetComponent<HeartBeat>();
        if (playerHeartbeat != null)
        {
            playerHeartbeat.StopHeartbeatEffect();
        }

        SwitchToJumpscareCamera();

        if (modelRenderer != null)
        {
            modelRenderer.enabled = false;
        }

        if (jumpscareObject != null)
        {
            jumpscareObject.SetActive(true);
        }

        if (jumpscareSound != null)
            AudioSource.PlayClipAtPoint(jumpscareSound, transform.position);
        if (animator != null && !string.IsNullOrEmpty(jumpscareAnimationTrigger))
            animator.SetTrigger(jumpscareAnimationTrigger);

        // Switch back to the main camera after the jumpscare is over
        Invoke(nameof(SwitchToMainCamera), jumpscareDuration);
    }

    public virtual void Stun(float duration)
    {
        TransitionToState(EnemyState.Stunned);
        agent.isStopped = true;
        Invoke(nameof(RecoverFromStun), duration);
    }

    public void ApplySpeedBoost(float amount, float duration)
    {
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }
        speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(amount, duration));
    }

    private IEnumerator SpeedBoostCoroutine(float amount, float duration)
    {
        speed = originalSpeed + amount;
        if (agent != null) agent.speed = speed;

        yield return new WaitForSeconds(duration);

        speed = originalSpeed;
        if (agent != null) agent.speed = speed;
        speedBoostCoroutine = null;
    }

    public void SetChaseTarget(Vector3 targetPosition)
    {
        TransitionToState(EnemyState.Chasing);
        if (agent != null)
        {
            agent.SetDestination(targetPosition);
        }
    }

    public void ChasePlayerForDuration(float duration)
    {
        if (persistentChaseCoroutine != null)
        {
            StopCoroutine(persistentChaseCoroutine);
        }
        persistentChaseCoroutine = StartCoroutine(PersistentChaseCoroutine(duration));
    }

    private IEnumerator PersistentChaseCoroutine(float duration)
    {
        float timer = duration;
        while (timer > 0)
        {
            if (player != null && currentState != EnemyState.Stunned && currentState != EnemyState.Attacking)
            {
                TransitionToState(EnemyState.Chasing);
                agent.SetDestination(player.position);
            }
            timer -= Time.deltaTime;
            yield return null;
        }
        persistentChaseCoroutine = null;
    }

    protected virtual void RecoverFromStun()
    {
        TransitionToState(EnemyState.Idle);
        agent.isStopped = false;
    }

    protected void SwitchToJumpscareCamera()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        if (jumpscareCamera != null) jumpscareCamera.gameObject.SetActive(true);
    }

    protected virtual void SwitchToMainCamera()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        if (jumpscareCamera != null) jumpscareCamera.gameObject.SetActive(false);

        if (jumpscareObject != null)
        {
            jumpscareObject.SetActive(false);
        }

        if (modelRenderer != null)
        {
            modelRenderer.enabled = true;
        }

        if (playerMovement != null) playerMovement.SetInputActive(true);

        // Re-enable the heartbeat effect on the player after respawning
        HeartBeat playerHeartbeat = player?.GetComponent<HeartBeat>();
        if (playerHeartbeat != null)
        {
            playerHeartbeat.EnableHeartbeatEffect();
        }

        Respawn();
        
        // Reset the flag and agent after the sequence is over
        isJumpscaring = false;
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }
}