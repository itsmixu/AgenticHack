using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance;

    public NPC currentNPC;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        NPC npc = collision.GetComponent<NPC>();
        if (npc != null)
        {
            currentNPC = npc;
            npc.playerNearby = true;
            Debug.Log("Near NPC: " + npc.name);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        NPC npc = collision.GetComponent<NPC>();
        if (npc != null && currentNPC == npc)
        {
            npc.playerNearby = false;
            currentNPC = null;
        }
    }
}