using UnityEngine;

public class WhiteClown : Enemy
{
    [Header("WhiteClown Specifics")]
    public float distortionDuration = 10f;
    public float distortionIntensity = 1.5f;

    [Header("State Animations")]
    public AnimationClip idleAnimation;
    public AnimationClip patrolAnimation;
    public AnimationClip chaseAnimation;
    public AnimationClip attackAnimation;

    private CameraDistortion cameraDistortion;

    protected override void Start()
    {
        base.Start();

        // Set clown-specific attributes
        speed = 5.0f; // Faster than the average enemy
        visionDistance = 15.0f;
        fieldOfView = 120.0f;

        // Find the CameraDistortion script on the player
        if (player != null)
        {
            cameraDistortion = player.GetComponent<CameraDistortion>();
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

        // Only play the animation if it's not already playing.
        // This prevents the animation from restarting every frame.
        if (clipToPlay != null)
        {
            // We use GetCurrentAnimatorStateInfo to check the active animation state.
            // The "0" refers to the base layer of the animator.
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName(clipToPlay.name))
            {
                animator.Play(clipToPlay.name);
            }
        }
        else
        {
            // Fallback to the base implementation if a clip isn't assigned
            base.UpdateAnimator();
        }
    }

    protected override void OnPlayerCaught()
    {
        // Trigger the standard jumpscare sequence from the base Enemy class
        TriggerJumpscare();

        // Also, trigger the camera distortion effect
        if (cameraDistortion != null)
        {
            cameraDistortion.StartDistortion(distortionDuration, distortionIntensity);
        }
    }
}