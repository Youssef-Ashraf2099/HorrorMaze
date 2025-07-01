using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public enum EnemyState
{
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Stunned
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
    public AudioClip[] enemySounds; // Footsteps, idle, chase, etc.
    public AudioClip jumpscareSound; // Dedicated jumpscare sound
    public GameObject jumpscareObject; // Jumpscare object (3D model, canvas, etc.)
    public string jumpscareAnimationTrigger = "Jumpscare"; // Animator trigger for jumpscare
    public Animator animator;
    public Transform[] patrolPoints;
    public Camera mainCamera;
    public Camera jumpscareCamera;
    public float jumpscareDuration = 2.5f;

    [Header("State")]
    public EnemyState currentState = EnemyState.Idle;
    protected float lastAttackTime;

    protected NavMeshAgent agent;
    private int currentPatrolIndex = 0;
    protected Transform player;
    protected playerMovment playerMovement; // Add this line
    protected Renderer modelRenderer;

    protected Vector3 initialPosition;
    protected Quaternion initialRotation;

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
    }
    protected virtual void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        agent = GetComponent<NavMeshAgent>(); // Add this line
        agent.speed = speed;                  // Also set the agent's speed
        modelRenderer = GetComponentInChildren<Renderer>();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // --- THEN, get the movement component from the player ---
        if (player != null)
        {
            playerMovement = player.GetComponent<playerMovment>();
        }
        if (player == null)
        {
            Debug.LogWarning($"{name}: Player not found in the scene. Enemy will not function properly.");
        }
        if (animator == null)
        {
            Debug.LogWarning($"{name}: Animator not assigned. Animations will not play.");
        }
        if (enemySounds == null || enemySounds.Length == 0)
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
    }

    protected virtual void Update()
    {
        if (player == null || currentState == EnemyState.Stunned)
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
        currentState = EnemyState.Idle;
    }
    protected virtual void IdleBehavior()
    {
        if (CanSeePlayer() || Vector3.Distance(transform.position, player.position) < aggroRange)
        {
            currentState = EnemyState.Chasing;
        }
        else if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentState = EnemyState.Patrolling;
        }
    }

    protected virtual void PatrolBehavior()
    {
        if (CanSeePlayer() || Vector3.Distance(transform.position, player.position) < aggroRange)
        {
            currentState = EnemyState.Chasing;
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
                currentState = EnemyState.Idle;
            }
        }
    }

    protected virtual void ChaseBehavior()
    {
        if (!CanSeePlayer() && Vector3.Distance(transform.position, player.position) > aggroRange)
        {
            currentState = (patrolPoints != null && patrolPoints.Length > 0) ? EnemyState.Patrolling : EnemyState.Idle;
            return;
        }

        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) < attackRange)
        {
            currentState = EnemyState.Attacking;
        }
    }

    protected virtual void AttackBehavior()
    {
        agent.ResetPath();
        transform.LookAt(player);

        if (Time.time > lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            OnPlayerCaught();
        }
        else
        {
            currentState = EnemyState.Chasing;
        }
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

    protected void PlaySound(int index)
    {
        if (enemySounds != null && index >= 0 && index < enemySounds.Length)
        {
            AudioSource.PlayClipAtPoint(enemySounds[index], transform.position);
        }
    }

    public virtual void TriggerJumpscare()
    {
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
        currentState = EnemyState.Stunned;
        agent.isStopped = true;
        Invoke(nameof(RecoverFromStun), duration);
    }

    protected virtual void RecoverFromStun()
    {
        currentState = EnemyState.Idle;
        agent.isStopped = false;
    }

    protected void SwitchToJumpscareCamera()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        if (jumpscareCamera != null) jumpscareCamera.gameObject.SetActive(true);
    }

    protected void SwitchToMainCamera()
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
    }
}