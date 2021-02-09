using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWallet : MonoBehaviour
{
    bool isServerSide = false;

    [SerializeField]
    int money = 0; //How much money the player has at the start

    PlayerInventory playerInventory;

    Text textComponent; //Text component to update to position.

    [SerializeField]
    string prefix = "Coins: ";

    /// <summary>
    /// Amount of money a player has.
    /// </summary>
    public int Money { get => money; }

    /// <summary>
    /// Check if the player can purchase.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool CanPurchase(int amount)
    {
        return amount <= money;
    }

    /// <summary>
    /// Do a purchase transaction for an object.
    /// </summary>
    /// <param name="cost">How much it costs.</param>
    /// <param name="item">What item would be purchased</param>
    /// <returns></returns>
    public bool Purchase(int cost, ITEM_TYPE item)
    {
        if (CanPurchase(cost))
        {
            playerInventory.AddItem(item);
            money -= cost;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }

    //Find the player inventory
    void Start()
    {
        // Get the instance of the player invenotry
        playerInventory = transform.parent.gameObject.GetComponentInChildren<PlayerInventory>();

        /* Check if serverside or not */
        if (gameObject.GetComponentInParent<NetworkPlayerInput>() != null)
        {
            isServerSide = true;
            return; //Dont check for the text component.
        }

        textComponent = GameObject.FindGameObjectWithTag("MoneyText").GetComponent<Text>();
        if (textComponent == null)
            throw new System.Exception("Text component not found at PlayerWallet.");
    }

    void Update()
    {
        if (!isServerSide) //Update text if not serverside
        {
            //Update text componet with amount of money.
            textComponent.text = prefix + money.ToString();
        }
    }
}
