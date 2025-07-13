using UnityEngine;
using System.Collections;

public class bebsGroup : Enemy
{
    [Header("bebsGroup Specifics")]
    public float rotationSpeed = 5f;

    [Header("State Animations")]
    public AnimationClip idleAnimation;
    public AnimationClip patrolAnimation;
    public AnimationClip chaseAnimation;
    public AnimationClip attackAnimation;

    protected override void Start()
    {
        base.Start(); // Ensures the base Enemy class initializes correctly.

        // Programmatically disable the NavMeshAgent's automatic rotation and position updates.
        // This gives our script full control via OnAnimatorMove.
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
    }

    /// <summary>
    /// This method is called by Unity when the Animator processes root motion.
    /// We use it to apply the NavMeshAgent's desired movement and our custom rotation.
    /// This is the most reliable way to sync scripted movement with animations.
    /// </summary>
    private void OnAnimatorMove()
    {
        if (agent == null || !agent.enabled) return;

        // Apply the NavMeshAgent's calculated velocity to the character's position
        transform.position = agent.nextPosition;

        // The rotation logic is now handled here during the chase state
        if (currentState == EnemyState.Chasing && player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    /// <summary>
    /// Overrides the base OnPlayerCaught behavior.
    /// When a bebsGroup enemy catches the player, it triggers a jumpscare,
    /// then alerts all other enemies to the player's location and gives them a speed boost for 15 seconds.
    /// </summary>
    protected override void OnPlayerCaught()
    {
        // First, trigger the jumpscare for this enemy.
        TriggerJumpscare();

        // If the player reference is valid, notify all other enemies.
        if (player != null)
        {
            // Iterate through all enemies in the scene.
            foreach (Enemy enemy in AllEnemies)
            {
                // Apply a speed boost of +2 for 15 seconds.
                enemy.ApplySpeedBoost(2f, 15f);

                // Make other enemies (not stunned or already attacking) start chasing the player.
                if (enemy != this)
                {
                    enemy.ChasePlayerForDuration(15f);
                }
            }
        }
    }

    // The ChaseBehavior no longer needs rotation logic, as it's handled in OnAnimatorMove.
    protected override void ChaseBehavior()
    {
        if (player == null) return;

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

    protected override void UpdateAnimator()
    {
        if (animator == null) return;

        AnimationClip clipToPlay = null;
        switch (currentState)
        {
            case EnemyState.Idle:
                clipToPlay = idleAnimation;
                break;
            case EnemyState.Patrolling:
                clipToPlay = patrolAnimation;
                break;
            case EnemyState.Chasing:
                clipToPlay = chaseAnimation;
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
        else
        {
            base.UpdateAnimator();
        }
    }
}