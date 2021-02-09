using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

//For storing inventory items
[Serializable]
public class InventoryItem
{
    [SerializeField]
    ITEM_TYPE itemType;

    [SerializeField]
    Sprite sprite;

    int amount = 0;

    //Texture of the item
    public Sprite Sprite { get => sprite; set => sprite = value; }
    //How much of the item is there
    public int Amount { get => amount; set => amount = value; }
    //What is the type of the item
    public ITEM_TYPE ItemType { get => itemType; set => itemType = value; }
}

