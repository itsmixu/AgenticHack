using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;  // Assign your scriptable object item here in inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Inventory playerInventory = other.GetComponent<Inventory>();
            if (playerInventory != null)
            {
                playerInventory.AddItem(itemData);
                Destroy(gameObject);
            }
        }
    }
}