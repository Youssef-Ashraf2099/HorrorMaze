using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int coinCount = 0;
    public int totalCoins = 10;
    public TMP_Text coinText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateCoinUI();
    }

    public void AddCoin()
    {
        coinCount++;
        UpdateCoinUI();
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = $"{coinCount}/{totalCoins}";
    }
}
