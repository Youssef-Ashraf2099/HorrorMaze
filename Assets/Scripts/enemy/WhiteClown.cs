using UnityEngine;
using System.Collections;

public class WhiteClown : Enemy
{
    [Header("WhiteClown Specifics")]
    public float dizzyDuration = 15f; // Set to 15 seconds
    public float distortionIntensity = 2.5f; // Increased intensity for a stronger effect
    private AudioSource jumpscareAudioSource;

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

        // Add and configure the AudioSource for the jumpscare
        jumpscareAudioSource = gameObject.AddComponent<AudioSource>();
        jumpscareAudioSource.playOnAwake = false;
        if (jumpscareSound != null)
        {
            jumpscareAudioSource.clip = jumpscareSound;
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

    protected override void OnPlayerCaught()
    {
        // Decrement player lives
        if (playerSanity != null)
        {
            playerSanity.PlayerCaught();
        }

        StartCoroutine(ClownJumpscareSequence());
    }

    private IEnumerator ClownJumpscareSequence()
    {
        // 1. Disable player input and hide the main enemy model
        if (playerMovement != null) playerMovement.SetInputActive(false);
        if (modelRenderer != null) modelRenderer.enabled = false;

        // 2. Trigger visual and audio effects
        SwitchToJumpscareCamera();
        if (jumpscareObject != null) jumpscareObject.SetActive(true);
        if (jumpscareAudioSource != null) jumpscareAudioSource.Play();
        if (animator != null && !string.IsNullOrEmpty(jumpscareAnimationTrigger))
        {
            animator.SetTrigger(jumpscareAnimationTrigger);
        }

        // 3. Start the camera distortion effect
        if (cameraDistortion != null)
        {
            cameraDistortion.StartDistortion(dizzyDuration, distortionIntensity);
        }

        // 4. Wait for the standard jumpscare to finish
        yield return new WaitForSeconds(jumpscareDuration);

        // 5. Stop the jumpscare sound
        if (jumpscareAudioSource != null && jumpscareAudioSource.isPlaying)
        {
            jumpscareAudioSource.Stop();
        }

        // 6. Switch back to the main camera and clean up
        SwitchToMainCamera();
        if (jumpscareObject != null) jumpscareObject.SetActive(false);
        if (modelRenderer != null) modelRenderer.enabled = true;

        // 7. Respawn the enemy
        Respawn();

        // 8. Wait for the remaining duration before re-enabling input
        float remainingDizzyTime = dizzyDuration - jumpscareDuration;
        if (remainingDizzyTime > 0)
        {
            yield return new WaitForSeconds(remainingDizzyTime);
        }

        // 9. Re-enable player input
        if (playerMovement != null) playerMovement.SetInputActive(true);
    }

    // Override to prevent base class from enabling input too early
    protected override void SwitchToMainCamera()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        if (jumpscareCamera != null) jumpscareCamera.gameObject.SetActive(false);
    }
}