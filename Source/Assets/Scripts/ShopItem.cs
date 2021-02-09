using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class ShopItem : MonoBehaviour
{
    [SerializeField]
    ITEM_TYPE itemType = ITEM_TYPE.NONE;

    [SerializeField]
    int cost = 0;

    bool isSold = false;
    SpriteRenderer spriteRenderer = null;

    // Try to find components and verify that the values are not silly.
    void Start()
    {
        if (itemType == ITEM_TYPE.NONE)
            Debug.LogError("ShopItem type set to none! At ShopItem.");
        if (cost == 0)
            Debug.LogError("ShopItem cost set to zero! At ShopItem.");
        if (cost < 0)
        {
            Debug.LogError("ShopItem cost is less than zero! No refunds! At ShopItem.");
            cost = 0;
        }

        if (!TryGetComponent<SpriteRenderer>(out spriteRenderer))
        {
            Debug.LogError("Unable to find SpriteRenderer! At ShopItem.");
            return;
        }

    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("LocalPlayer"))
        {
            ////TODO: PLAYER ID FOR NETWORKING
            var playerWallet = col.gameObject.GetComponentInChildren<PlayerWallet>();
            if (playerWallet.CanPurchase(cost))
            {
                playerWallet.Purchase(cost, itemType);
                isSold = true;
                gameObject.GetComponent<BoxCollider2D>().enabled = false;
                gameObject.GetComponent<SpriteRenderer>().color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
            }
                
        }
    }
}
