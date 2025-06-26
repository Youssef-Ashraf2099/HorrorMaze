using UnityEngine;

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
    [Header("Enemy Attributes")]
    public float speed = 3.0f;
    public float visionDistance = 10.0f;
    public float fieldOfView = 120.0f;
    public float aggroRange = 8.0f;
    public float attackCooldown = 2.0f;
    public AudioClip[] enemySounds; // Footsteps, idle, chase, jumpscare, etc.
    public Animator animator;
    public Transform[] patrolPoints;
    public Camera mainCamera;
    public Camera jumpscareCamera;


    [Header("State")]
    public EnemyState currentState = EnemyState.Idle;
    protected float lastAttackTime;

    protected Transform player;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
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
    }

    protected virtual void Update()
    {
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
            case EnemyState.Stunned:
                StunnedBehavior();
                break;
        }
    }

    protected abstract void OnPlayerCaught();

    protected virtual void IdleBehavior() { }
    protected virtual void PatrolBehavior() { }
    protected virtual void ChaseBehavior() { }
    protected virtual void AttackBehavior() { }
    protected virtual void StunnedBehavior() { }

    protected bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 directionToPlayer = player.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (directionToPlayer.magnitude < visionDistance && angle < fieldOfView * 0.5f)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, directionToPlayer.normalized, out hit, visionDistance))
            {
                if (hit.transform == player)
                    return true;
            }
        }
        return false;
    }

    protected void PlaySound(int index)
    {
        if (enemySounds == null || enemySounds.Length == 0)
        {
            Debug.LogWarning($"{name}: No enemy sounds assigned. Cannot play sound.");
            return;
        }
        if (index < 0 || index >= enemySounds.Length)
        {
            Debug.LogWarning($"{name}: Sound index {index} is out of range.");
            return;
        }
        AudioSource.PlayClipAtPoint(enemySounds[index], transform.position);
    }

    public virtual void TriggerJumpscare()
    {
        SwitchToJumpscareCamera();
        // To be implemented in subclass or extended here
    }

    public virtual void Stun(float duration)
    {
        currentState = EnemyState.Stunned;
        Invoke(nameof(RecoverFromStun), duration);
    }

    protected virtual void RecoverFromStun()
    {
        currentState = EnemyState.Idle;
    }
    protected void SwitchToJumpscareCamera()
    {
        if (mainCamera != null) mainCamera.enabled = false;
        if (jumpscareCamera != null) jumpscareCamera.enabled = true;
    }

    protected void SwitchToMainCamera()
    {
        if (mainCamera != null) mainCamera.enabled = true;
        if (jumpscareCamera != null) jumpscareCamera.enabled = false;
    }

}
