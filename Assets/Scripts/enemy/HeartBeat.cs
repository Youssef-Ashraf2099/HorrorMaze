using UnityEngine;

public class HeartBeat : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("The AudioSource component dedicated to playing the heartbeat sound.")]
    public AudioSource heartbeatSource;

    [Header("Animation")]
    [Tooltip("The Animator component to control for the visual heartbeat effect.")]
    public Animator heartbeatAnimator;

    [Tooltip("The name of the float parameter in the Animator to control the effect's intensity.")]
    public string intensityParameterName = "HeartbeatIntensity";

    [Header("Heartbeat Settings")]
    [Tooltip("The audio clip of the heartbeat sound.")]
    public AudioClip heartbeatSound;

    [Tooltip("The maximum distance at which the heartbeat starts.")]
    public float maxDistance = 20f;

    [Tooltip("The minimum distance at which the heartbeat is at its fastest and loudest.")]
    public float minDistance = 1f;

    [Header("Audio Pitch")]
    [Tooltip("The pitch of the heartbeat at maximum distance (slowest).")]
    public float minPitch = 0.8f;

    [Tooltip("The pitch of the heartbeat at minimum distance (fastest).")]
    public float maxPitch = 2.0f;

    [Header("Audio Volume")]
    [Tooltip("The volume of the heartbeat at maximum distance.")]
    public float minVolume = 0.1f;

    [Tooltip("The volume of the heartbeat at minimum distance.")]
    public float maxVolume = 1.0f;

    private bool isHeartbeatActive = true;

    void Start()
    {
        if (heartbeatSource == null)
        {
            Debug.LogError("Heartbeat AudioSource is not assigned in the Inspector!", this);
            return;
        }

        if (heartbeatAnimator == null)
        {
            Debug.LogWarning("Heartbeat Animator is not assigned. Visual effect will not play.", this);
        }

        // Configure the AudioSource for the heartbeat effect
        heartbeatSource.clip = heartbeatSound;
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.volume = 0; // Start with no volume
        heartbeatSource.pitch = minPitch;
    }

    void Update()
    {
        if (!isHeartbeatActive || heartbeatSource == null) return; // Stop if not active or source is not assigned

        float closestDistance = float.MaxValue;
        Enemy closestEnemy = null;

        // Find the closest enemy from the static list
        foreach (var enemy in Enemy.AllEnemies)
        {
            if (enemy == null) continue; // Skip if an enemy was destroyed but not yet removed from the list
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        // If an enemy is found within the maxDistance
        if (closestEnemy != null && closestDistance <= maxDistance)
        {
            // Ensure the sound is playing
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }

            // Calculate the intensity of the effect (0 to 1)
            float intensity = Mathf.InverseLerp(maxDistance, minDistance, closestDistance);

            // Apply the intensity to volume and pitch
            heartbeatSource.volume = Mathf.Lerp(minVolume, maxVolume, intensity);
            heartbeatSource.pitch = Mathf.Lerp(minPitch, maxPitch, intensity);

            // Also apply the intensity to the animator
            if (heartbeatAnimator != null)
            {
                heartbeatAnimator.SetFloat(intensityParameterName, intensity);
            }
        }
        else
        {
            // If no enemy is in range, fade out the sound
            if (heartbeatSource.isPlaying)
            {
                heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, 0f, Time.deltaTime * 2);
                if (heartbeatSource.volume < 0.01f)
                {
                    heartbeatSource.Stop();
                }
            }

            // Also fade out the animation, snapping to zero to ensure it stops
            if (heartbeatAnimator != null)
            {
                float currentIntensity = heartbeatAnimator.GetFloat(intensityParameterName);
                if (currentIntensity > 0)
                {
                    float newIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * 2);
                    if (newIntensity < 0.01f)
                    {
                        newIntensity = 0f; // Snap to zero to guarantee the animation stops
                    }
                    heartbeatAnimator.SetFloat(intensityParameterName, newIntensity);
                }
            }
        }
    }

    /// <summary>
    /// Immediately stops the heartbeat effect and prevents it from restarting until enabled again.
    /// </summary>
    public void StopHeartbeatEffect()
    {
        isHeartbeatActive = false;
        if (heartbeatSource != null)
        {
            heartbeatSource.Stop();
        }
        if (heartbeatAnimator != null)
        {
            heartbeatAnimator.SetFloat(intensityParameterName, 0f);
        }
    }

    /// <summary>
    /// Allows the heartbeat effect to run again.
    /// </summary>
    public void EnableHeartbeatEffect()
    {
        isHeartbeatActive = true;
    }
}