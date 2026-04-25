using UnityEngine;

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
        for (int i = 0; i < slots.Length; i++)
        {
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

}
