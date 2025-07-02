using UnityEngine;

public class Reeper : Enemy
{
    [Header("State Animations")]
    public AnimationClip idleAnimation;
    public AnimationClip patrolAnimation;
    public AnimationClip chaseAnimation;
    public AnimationClip attackAnimation;

    protected override void Start()
    {
        // Call the base Start method to initialize components
        base.Start();

        // Set Reeper-specific attributes if needed
        // For example, you might want to adjust its speed or vision
        speed = 4.0f;
        visionDistance = 12.0f;
        aggroRange = 10.0f;
    }

    /// <summary>
    /// This method is called when the Reeper catches the player.
    /// It overrides the base implementation to take two lives from the player.
    /// </summary>
    protected override void OnPlayerCaught()
    {
        // Check if the player's sanity component is available
        if (playerSanity != null)
        {
            // Decrement player's lives twice
            playerSanity.PlayerCaught();
            playerSanity.PlayerCaught();
        }

        // Trigger the standard jumpscare sequence from the base Enemy class
        TriggerJumpscare();
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

        // Only play the animation if it's not already playing.
        if (clipToPlay != null)
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(clipToPlay.name))
            {
                animator.Play(clipToPlay.name);
            }
        }
        else
        {
            // Fallback to the base animator logic if no specific clip is assigned
            base.UpdateAnimator();
        }
    }
}