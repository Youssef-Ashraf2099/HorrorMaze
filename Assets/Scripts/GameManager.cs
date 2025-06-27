using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject collectablePrefab; // Assign your collectable prefab in the Inspector
    public GameObject[] spawnPoints; // Assign your spawn point GameObjects in the Inspector
    public int numberOfCollectablesToSpawn = 5; // Set how many collectables you want to spawn
    public int coinCount = 0;
    public int totalCoins;
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
        SpawnCollectables();
        UpdateCoinUI();
    }

    void SpawnCollectables()
    {
        // If no spawn points are assigned, do nothing.
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned in the GameManager.");
            totalCoins = 0;
            return;
        }

        // Create a temporary list of spawn points to shuffle
        List<GameObject> randomizedSpawnPoints = new List<GameObject>(spawnPoints);

        // Shuffle the list using the Fisher-Yates algorithm
        int n = randomizedSpawnPoints.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1); // Use UnityEngine.Random
            GameObject value = randomizedSpawnPoints[k];
            randomizedSpawnPoints[k] = randomizedSpawnPoints[n];
            randomizedSpawnPoints[n] = value;
        }

        // Determine the number of collectables to spawn, ensuring it doesn't exceed available points
        totalCoins = Mathf.Min(numberOfCollectablesToSpawn, randomizedSpawnPoints.Count);

        // Spawn a collectable at the selected number of random spawn points
        for (int i = 0; i < totalCoins; i++)
        {
            GameObject spawnPoint = randomizedSpawnPoints[i];
            Instantiate(collectablePrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
        }
    }

    public void ResetAndRespawnCollectables()
    {
        // Find and destroy all existing collectables
        GameObject[] existingCollectables = GameObject.FindGameObjectsWithTag("Collectable");
        foreach (GameObject collectable in existingCollectables)
        {
            Destroy(collectable);
        }

        // Reset coin count and spawn new ones
        coinCount = 0;
        SpawnCollectables();
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