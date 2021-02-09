using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// For representing the players inventory.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    bool isServerSide = false;

    [SerializeField]
    List<InventoryItem> Inventory; //What the players inventory can contain, and how much of it.

    [SerializeField]
    GameObject parentItemSlotObject; //Where should the item slots get added to.

    [SerializeField]
    GameObject itemSlotPrefab; //Item slot prefab.


    // Start is called before the first frame update
    void Start()
    {
        /*Verify that you can add everything to the inventory*/
        if (Inventory.Count == 0)
        {
            Debug.LogError("Inventory has no things that it could contain (Invetory array empty) at PlayerInventory.");
            return;
        }

        /*If the player is serverside*/
        if (gameObject.GetComponentInParent<NetworkPlayerInput>() != null)
        {
            isServerSide = true;
            return; //Don't create GUI stuff.
        }

        /*Otherwise create GUI stuff*/
        parentItemSlotObject = GameObject.FindGameObjectWithTag("ItemSlotParent");
        if (parentItemSlotObject == null)
        {
            throw new System.Exception("Parent item slot not found at PlayerInventory (dynamically found).");
        }
        if (itemSlotPrefab == null)
        {
            throw new System.Exception("Item slot prefab not set at PlayerInventory (set in prefab).");
        }

        CreateInventoryGUI();
    }

    /// <summary>
    /// Creates the GUI for the inventory.
    /// </summary>
    private void CreateInventoryGUI()
    {
        int current = 0;
        foreach (var item in Inventory)
        {
            var createdItemSlot = GameObject.Instantiate(itemSlotPrefab, parentItemSlotObject.transform); //create new 
            createdItemSlot.transform.position = new Vector3(parentItemSlotObject.GetComponent<RectTransform>().rect.x + createdItemSlot.GetComponent<RectTransform>().rect.width * (float)current + createdItemSlot.GetComponent<RectTransform>().rect.width,
                30.0f, 
                parentItemSlotObject.transform.position.z);
            createdItemSlot.GetComponent<ToolbarItem>().InventoryItem = item;
            ++current;
        }
    }

    /// <summary>
    /// Add an item to the players inventory.
    /// </summary>
    /// <param name="itemType">What item to add.</param>
    /// <param name="amount">How much to add.</param>
    public void AddItem(ITEM_TYPE itemType, int amount=1)
    {
        //get all ite
        var inventoryItem = Inventory.Find(item => item.ItemType == itemType);
        if (inventoryItem == null)
        {
            Debug.LogError("Item " + itemType.ToString() + " Not found! At PlayerInventory.");
            return;
        }
        inventoryItem.Amount += amount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
