using UnityEngine;

public class basela : Enemy
{
    protected override void Start()
    {
        // Set ghost-specific attributes (can also be set in Inspector)
        speed = 4.5f; // Good speed
        visionDistance = 12.0f; // Moderate vision
        fieldOfView = 100.0f;   // Slightly narrower FOV

        base.Start();
    }

    protected override void OnPlayerCaught()
    {
        Debug.Log($"{name}: Player caught by basela! All keys lost and respawned.");

        // Call the GameManager to handle key reset and respawning
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetAndRespawnCollectables();
        }

        TriggerJumpscare();
        // Add additional effects here
    }

    public override void TriggerJumpscare()
    {
        base.TriggerJumpscare();
        // Play basela-specific jumpscare animation/sound here
        Debug.Log($"{name}: basela jumpscare triggered!");
        // After jumpscare, you might want to switch back to main camera:
        // SwitchToMainCamera();
    }
}
