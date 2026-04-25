using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public Image image;
    public TMP_Text itemName;

    public void SetItem(ItemData item)
    {
        image.sprite = item.image;
        image.enabled = true;
        itemName.text = item.itemName;
    }
    
    public void ClearSlot()
    {
        image.sprite = null;
        image.enabled = false;
        itemName.text = "";
    }
}
