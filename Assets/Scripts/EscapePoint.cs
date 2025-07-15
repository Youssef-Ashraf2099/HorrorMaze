using UnityEngine;

public class EscapePoint : MonoBehaviour
{
    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{

    //}

    //// Update is called once per frame
    //void Update()
    //{

    //}

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null && GameManager.Instance.coinCount >= GameManager.Instance.totalCoins)
            {
                Debug.Log("You have collected all the items! You escaped!");
                // You can add your scene-changing or game-ending logic here
            }
            else
            {
                Debug.Log("You need to collect all the items before you can escape.");
            }
        }
    }
}