using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public Inventory playerInventory; // Reference to inventory script
    public GameObject slotPrefab;
    public PlayerInteraction playerInteraction;

    private Transform slotsParent;
    private InventorySlotUI[] slots;

    private void Start()
    {
        slotsParent = transform;

        // Create slots in UI
        slots = new InventorySlotUI[5];
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, slotsParent);
            slots[i] = slotGO.GetComponent<InventorySlotUI>();
            slots[i].ClearSlot();
        }
        //UpdateUI();
    }

    private void Update()
    {
        // Listen for number keys 1 to 9
        for (int i = 0; i < slots.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                GiveItemToNPC(i);
            }

            if (i < playerInventory.items.Count)
                slots[i].SetItem(playerInventory.items[i]);
            else
                slots[i].ClearSlot();
        }
    }

    // Unused
    public void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < playerInventory.items.Count)
                slots[i].SetItem(playerInventory.items[i]);
            else
                slots[i].ClearSlot();
        }
    }

    private void GiveItemToNPC(int index)   
    {
        if (index < playerInventory.items.Count)
        {
            ItemData itemToGive = playerInventory.items[index];
            if (PlayerInteraction.Instance.currentNPC != null)
            {
                PlayerInteraction.Instance.currentNPC.ReceiveItem(itemToGive);
                playerInventory.RemoveItem(itemToGive);
                Debug.Log("Gave " + itemToGive.itemName + " to NPC " + PlayerInteraction.Instance.currentNPC.name);
            }
            else
            {
                Debug.Log("No NPC nearby to give item.");
            }
        }
    }
}
