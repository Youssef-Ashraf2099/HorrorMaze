using UnityEngine;
using System.Collections;

public class basela : Enemy
{
    private AudioSource jumpscareAudioSource;

    protected override void Start()
    {
        base.Start();

        // Set ghost-specific attributes (can also be set in Inspector)
        speed = 4.5f; // Good speed
        visionDistance = 12.0f; // Moderate vision
        fieldOfView = 100.0f;   // Slightly narrower FOV
                                // footstepInterval = 3.0f; // Set hover sound interval to 3 seconds

        // Add and configure the AudioSource for the jumpscare
        jumpscareAudioSource = gameObject.AddComponent<AudioSource>();
        jumpscareAudioSource.playOnAwake = false;
        if (jumpscareSound != null)
        {
            jumpscareAudioSource.clip = jumpscareSound;
        }
    }

    protected override void OnPlayerCaught()
    {
        // If a jumpscare is already happening, do nothing.
        if (isJumpscaring) return;

        // Set the flag immediately to stop all base class behaviors.
        isJumpscaring = true;

        // Decrement player lives
        if (playerSanity != null)
        {
            playerSanity.PlayerCaught();
        }

        Debug.Log($"{name}: Player caught by basela! All keys lost and respawned.");

        // Call the GameManager to handle key reset and respawning
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetAndRespawnCollectables();
        }

        // Start the jumpscare sequence
        StartCoroutine(BaselaJumpscareSequence());
    }

    private IEnumerator BaselaJumpscareSequence()
    {
        // 1. Stop all movement and base sounds immediately
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // 2. Disable player input and hide the main enemy model
        if (playerMovement != null) playerMovement.SetInputActive(false);
        if (modelRenderer != null) modelRenderer.enabled = false;

        // 3. Trigger visual and audio effects
        SwitchToJumpscareCamera();
        if (jumpscareObject != null) jumpscareObject.SetActive(true);
        if (jumpscareAudioSource != null) jumpscareAudioSource.Play();
        if (animator != null && !string.IsNullOrEmpty(jumpscareAnimationTrigger))
        {
            animator.SetTrigger(jumpscareAnimationTrigger);
        }

        // 4. Wait for the jumpscare to finish
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

        // 7. Respawn the enemy and re-enable player input
        Respawn();
        if (playerMovement != null) playerMovement.SetInputActive(true);

        // 8. Reset the state after the sequence is fully complete
        isJumpscaring = false;
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    protected override void UpdateAnimator()
    {
        // This is where you would put custom animation logic for basela.
        // For example, maybe it has a "FlyingSpeed" parameter instead of "Speed".
        // animator.SetFloat("FlyingSpeed", agent.velocity.magnitude);

        // If you want to use the base logic, you can call it directly:
        base.UpdateAnimator();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnPlayerCaught();
        }
    }

    public override void TriggerJumpscare()
    {
        // This override can be simplified or removed if the coroutine handles everything.
        // For now, we ensure it also respects the isJumpscaring flag.
        if (isJumpscaring) return;
        OnPlayerCaught();
        Debug.Log($"{name}: basela jumpscare triggered!");
    }
}