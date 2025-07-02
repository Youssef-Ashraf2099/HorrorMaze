using UnityEngine;
using TMPro;

public class Sanity : MonoBehaviour
{
    [Header("Life Settings")]
    [Tooltip("The number of lives the player starts with.")]
    public int maxLives = 3;

    [Tooltip("The UI Text element to display the current lives.")]
    public TextMeshProUGUI livesText;

    private int currentLives;

    void Start()
    {
        currentLives = maxLives;
        UpdateLivesUI();
    }

    void Update()
    {

    }

    /// <summary>
    /// Decrements the player's lives and updates the UI.
    /// This should be called by other scripts when the player is caught.
    /// </summary>
    public void PlayerCaught()
    {
        if (currentLives > 0)
        {
            currentLives--;
            UpdateLivesUI();

            if (currentLives <= 0)
            {
                Debug.Log("player lost");
                // You can add additional game over logic here,
                // such as pausing the game or loading a different scene.
            }
        }
    }

    /// <summary>
    /// Updates the lives display text.
    /// </summary>
    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = "Lives: " + currentLives;
        }
    }
}