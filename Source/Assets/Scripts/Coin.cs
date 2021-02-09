using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField]
    int value = 10;

    bool isCollected = false;

    // Start is called before the first frame update
    void Start()
    {
        if (value == 0)
            Debug.LogError("Coin worth nothing at Coin.");
        else if (value < 0)
            Debug.LogError("Coin gives a refund at Coin.");
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("LocalPlayer"))
        {
            var playerWallet = col.gameObject.GetComponentInChildren<PlayerWallet>();
            playerWallet.AddMoney(value);
            GetComponent<CircleCollider2D>().enabled = false;
            isCollected = true;
        }
    }

    void Update()
    {
        if (isCollected)
            GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        else
            GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    }
}
