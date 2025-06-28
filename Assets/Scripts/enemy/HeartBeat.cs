using UnityEngine;

public class HeartBeat : MonoBehaviour
{
    [Header("Audio Source")]
    [Tooltip("The AudioSource component dedicated to playing the heartbeat sound.")]
    public AudioSource heartbeatSource;

    [Header("UI and Animation")]
    [Tooltip("The UI panel GameObject that contains the heartbeat animation.")]
    public GameObject heartbeatPanel;

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
        // Ensure required components are assigned
        if (heartbeatSource == null)
        {
            Debug.LogError("Heartbeat AudioSource is not assigned in the Inspector!", this);
            return;
        }
        if (heartbeatPanel == null)
        {
            Debug.LogError("Heartbeat Panel is not assigned in the Inspector!", this);
            return;
        }
        if (heartbeatAnimator == null)
        {
            Debug.LogWarning("Heartbeat Animator is not assigned. Visual effect will not play.", this);
        }

        // Configure the AudioSource and disable the panel by default
        heartbeatSource.clip = heartbeatSound;
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatPanel.SetActive(false);
    }

    void Update()
    {
        if (!isHeartbeatActive || heartbeatSource == null) return;

        float closestDistance = float.MaxValue;
        Enemy closestEnemy = null;

        // Find the closest enemy
        foreach (var enemy in Enemy.AllEnemies)
        {
            if (enemy == null) continue;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        // If an enemy is in range
        if (closestEnemy != null && closestDistance <= maxDistance)
        {
            // Enable the panel and start the sound if they aren't already active
            if (!heartbeatPanel.activeSelf)
            {
                heartbeatPanel.SetActive(true);
            }
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }

            // Calculate and apply the intensity to the sound and animation speed
            float intensity = Mathf.InverseLerp(maxDistance, minDistance, closestDistance);
            heartbeatSource.volume = Mathf.Lerp(minVolume, maxVolume, intensity);
            heartbeatSource.pitch = Mathf.Lerp(minPitch, maxPitch, intensity);
            if (heartbeatAnimator != null)
            {
                heartbeatAnimator.SetFloat(intensityParameterName, intensity);
            }
        }
        else
        {
            // If no enemy is in range, disable the panel and stop the sound
            if (heartbeatPanel.activeSelf)
            {
                heartbeatPanel.SetActive(false);
            }
            if (heartbeatSource.isPlaying)
            {
                heartbeatSource.Stop();
            }
        }
    }

    public void StopHeartbeatEffect()
    {
        isHeartbeatActive = false;
        if (heartbeatSource != null && heartbeatSource.isPlaying)
        {
            heartbeatSource.Stop();
        }
        if (heartbeatPanel != null && heartbeatPanel.activeSelf)
        {
            heartbeatPanel.SetActive(false);
        }
    }

    public void EnableHeartbeatEffect()
    {
        isHeartbeatActive = true;
    }
}