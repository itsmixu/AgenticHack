using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();

    // Add item to inventory
    public void AddItem(ItemData item)
    {
        items.Add(item);
        Debug.Log("Added " + item.itemName);
    }

    // Remove item from inventory
    public void RemoveItem(ItemData item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            Debug.Log("Removed " + item.itemName);
        }
    }
}
