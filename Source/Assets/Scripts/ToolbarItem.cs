using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ToolbarItem : MonoBehaviour
{
    public InventoryItem InventoryItem; // Reference to the inventory item in another class
    Image imageComponent; //Texture it will render.
    Text amountTextComponent; //Text component which will show how much of the said object there is.

    // Setup components
    void Start()
    {
        if (InventoryItem == null)
            Debug.LogError("InventoryItem is null at ToolbarItem.");
        else
        {
            if (InventoryItem.Sprite == null)
                Debug.LogError("InventoryItem sprite is null! at ToolbarItem.");
            if (InventoryItem.ItemType == ITEM_TYPE.NONE)
                Debug.LogError("InventoryItem ItemType is None! (should be not none) at ToolbarItem.");
        }

        imageComponent = GetComponentInChildren<Image>();

        if (imageComponent == null)
            Debug.LogError("Unable to find image component at ToolbarItem.");

        amountTextComponent = GetComponentInChildren<Text>();

        if (amountTextComponent == null)
            Debug.LogError("Unable to find the text component for amount at ToolbarItem.");

        imageComponent.sprite = InventoryItem.Sprite;
    }

    // Update amount text
    void Update()
    {
        amountTextComponent.text = InventoryItem.Amount.ToString();
    }
}
