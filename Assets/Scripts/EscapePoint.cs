using UnityEngine;
using TMPro; // Add this line to use TextMeshPro
using System.Collections; // Add this line to use Coroutines

public class EscapePoint : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField]
    private GameObject interactPrompt; // Assign a UI GameObject that says "Press E to Interact"
    [SerializeField]
    private TMP_Text messageText; // Assign a TextMeshPro UI element for messages

    private Coroutine messageCoroutine;

    void Start()
    {
        // Ensure UI is hidden at the start
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(true);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            if (GameManager.Instance != null && GameManager.Instance.coinCount >= GameManager.Instance.totalCoins)
            {
                Debug.Log("You have collected all the items! You escaped!");
                // You can add your scene-changing or game-ending logic here
            }
            else
            {
                // Stop any previous message coroutine to avoid overlap
                if (messageCoroutine != null)
                {
                    StopCoroutine(messageCoroutine);
                }
                messageCoroutine = StartCoroutine(ShowMessage("You need to collect all the items before you can escape.", 3f));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Hide prompt and any active messages when player leaves
            if (interactPrompt != null)
            {
                interactPrompt.SetActive(false);
            }
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageText.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator ShowMessage(string message, float duration)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            messageText.gameObject.SetActive(false);
        }
    }
}