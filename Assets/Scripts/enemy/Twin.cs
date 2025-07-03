using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Twin : Enemy
{
    [Header("Twin Specifics")]
    [Tooltip("Duration of the hallucination effect in seconds.")]
    public float hallucinationDuration = 10f;

    [Tooltip("The UI Image used for the vision-blocking effect.")]
    public Image visionOverlay;

    [Tooltip("Voice clips to play during the hallucination.")]
    public AudioClip[] hallucinationVoices;

    [Header("State Animations")]
    public AnimationClip idleAnimation;
    public AnimationClip patrolAnimation;
    public AnimationClip chaseAnimation;
    public AnimationClip attackAnimation;

    private AudioSource hallucinationAudioSource;
    private static int activeHallucinations = 0;

    protected override void Start()
    {
        base.Start();

        // Configure the dedicated AudioSource for hallucination sounds
        hallucinationAudioSource = gameObject.AddComponent<AudioSource>();
        hallucinationAudioSource.playOnAwake = false;
        hallucinationAudioSource.loop = true;
        hallucinationAudioSource.ignoreListenerPause = true; // Ensures voices play when other sounds are paused

        if (visionOverlay == null)
        {
            Debug.LogWarning("Twin: Vision Overlay has not been assigned in the Inspector. The visual hallucination effect will not be visible.");
        }
    }

    /// <summary>
    /// Overrides the base animator logic to play specific animation clips based on the current state.
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

    /// <summary>
    /// Overrides the base method to trigger a custom hallucination sequence.
    /// </summary>
    protected override void OnPlayerCaught()
    {
        if (playerSanity != null)
        {
            playerSanity.PlayerCaught();
        }

        // This coroutine now handles both the jumpscare and the hallucination.
        StartCoroutine(JumpscareThenHallucinationSequence());
    }

    /// <summary>
    /// Manages a sequential jumpscare and hallucination effect.
    /// </summary>
    private IEnumerator JumpscareThenHallucinationSequence()
    {
        // --- Part 1: Jumpscare (2.5 seconds) ---
        TriggerJumpscare();
        yield return new WaitForSeconds(jumpscareDuration);

        // --- Part 2: Hallucination (10 seconds) ---
        // Clean up jumpscare visuals but keep player input frozen.
        if (jumpscareObject != null) jumpscareObject.SetActive(false);
        SwitchToMainCamera(); // Switch camera back, but input is still disabled.

        // Start hallucination effects
        activeHallucinations++;
        AudioListener.pause = true;

        if (visionOverlay != null)
        {
            visionOverlay.gameObject.SetActive(true);
            visionOverlay.color = new Color(0, 0, 0, 0.85f);
        }

        if (hallucinationVoices != null && hallucinationVoices.Length > 0)
        {
            hallucinationAudioSource.clip = hallucinationVoices[Random.Range(0, hallucinationVoices.Length)];
            hallucinationAudioSource.Play();
        }

        yield return new WaitForSeconds(hallucinationDuration);

        // --- Part 3: Cleanup ---
        if (hallucinationAudioSource.isPlaying)
        {
            hallucinationAudioSource.Stop();
        }

        activeHallucinations--;

        if (activeHallucinations == 0)
        {
            if (visionOverlay != null)
            {
                visionOverlay.gameObject.SetActive(false);
            }
            AudioListener.pause = false;
            if (playerMovement != null) playerMovement.SetInputActive(true); // Re-enable input now.
        }

        Respawn();
        if (modelRenderer != null) modelRenderer.enabled = true;
    }

    // Override to prevent the base jumpscare from ending too early or re-enabling input
    public override void TriggerJumpscare()
    {
        if (playerMovement != null) playerMovement.SetInputActive(false);

        SwitchToJumpscareCamera();

        if (modelRenderer != null) modelRenderer.enabled = false;
        if (jumpscareObject != null) jumpscareObject.SetActive(true);
        if (jumpscareSound != null) AudioSource.PlayClipAtPoint(jumpscareSound, transform.position);
        if (animator != null && !string.IsNullOrEmpty(jumpscareAnimationTrigger))
        {
            animator.SetTrigger(jumpscareAnimationTrigger);
        }
    }

    // Override to prevent the base method from re-enabling input too early
    protected override void SwitchToMainCamera()
    {
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        if (jumpscareCamera != null) jumpscareCamera.gameObject.SetActive(false);
        // NOTE: We do NOT re-enable player input here. The coroutine controls it.
    }
}