using UnityEngine;

public class HeartBeat : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("The AudioSource component dedicated to playing the heartbeat sound.")]
    public AudioSource heartbeatSource;

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

    void Start()
    {
        if (heartbeatSource == null)
        {
            Debug.LogError("Heartbeat AudioSource is not assigned in the Inspector!", this);
            return;
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
        if (heartbeatSource == null) return; // Stop if the source is not assigned

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
            // InverseLerp gives us a value of 1 at minDistance and 0 at maxDistance
            float intensity = Mathf.InverseLerp(maxDistance, minDistance, closestDistance);

            // Apply the intensity to volume and pitch
            heartbeatSource.volume = Mathf.Lerp(minVolume, maxVolume, intensity);
            heartbeatSource.pitch = Mathf.Lerp(minPitch, maxPitch, intensity);
        }
        else
        {
            // If no enemy is in range, fade out the sound instead of stopping abruptly
            if (heartbeatSource.isPlaying)
            {
                heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, 0f, Time.deltaTime * 2);
                if (heartbeatSource.volume < 0.01f)
                {
                    heartbeatSource.Stop();
                }
            }
        }
    }
}