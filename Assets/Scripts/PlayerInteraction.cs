using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance;

    public NPC currentNPC;
    [SerializeField] private InteractionPopupUI interactionPopupUI;

    [Header("Player State")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth;

    private Inventory inventory;
    private bool isInteractionActive;
    private bool isPlayerTurn;

    public bool IsInteractionActive => isInteractionActive;
    public bool IsPlayerTurn => isPlayerTurn;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        inventory = GetComponent<Inventory>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (currentNPC == null || DialogueManager.Instance.isDialogueActive)
            return;

        if (!isInteractionActive && Input.GetKeyDown(KeyCode.E))
        {
            BeginInteraction();
        }
    }

    public void BeginInteraction()
    {
        if (currentNPC == null || isInteractionActive)
            return;

        isInteractionActive = true;
        isPlayerTurn = true;
        PlayerMovement.Instance.canMove = false;

        if (interactionPopupUI != null)
        {
            interactionPopupUI.Show();
            interactionPopupUI.SetTurnState(true);
        }
    }

    public void EndInteraction()
    {
        if (!isInteractionActive)
            return;

        isInteractionActive = false;
        isPlayerTurn = false;
        PlayerMovement.Instance.canMove = true;

        if (interactionPopupUI != null)
        {
            interactionPopupUI.Hide();
        }
    }

    public void SubmitTalk(string message)
    {
        if (!CanAct())
            return;

        StartCoroutine(ProcessNpcTurn(currentNPC.ProcessPlayerAction(PlayerActionType.Talk, message, null)));
    }

    public void SubmitGiveItem(string itemName)
    {
        if (!CanAct())
            return;

        if (inventory == null)
        {
            if (interactionPopupUI != null)
                interactionPopupUI.SetStatus("Player inventory not found.");
            return;
        }

        ItemData itemToGive = null;
        for (int i = 0; i < inventory.items.Count; i++)
        {
            ItemData item = inventory.items[i];
            if (item != null && item.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                itemToGive = item;
                break;
            }
        }

        if (itemToGive == null)
        {
            if (interactionPopupUI != null)
                interactionPopupUI.SetStatus("Item not found in inventory.");
            return;
        }

        inventory.RemoveItem(itemToGive);
        StartCoroutine(ProcessNpcTurn(currentNPC.ProcessPlayerAction(PlayerActionType.GiveItem, null, itemToGive)));
    }

    public void SubmitHit()
    {
        if (!CanAct())
            return;

        StartCoroutine(ProcessNpcTurn(currentNPC.ProcessPlayerAction(PlayerActionType.Hit, null, null)));
    }

    private bool CanAct()
    {
        return isInteractionActive && isPlayerTurn && currentNPC != null;
    }

    private System.Collections.IEnumerator ProcessNpcTurn(NpcInteractionResponse response)
    {
        isPlayerTurn = false;
        if (interactionPopupUI != null)
            interactionPopupUI.SetTurnState(false);

        if (response != null)
        {
            if (!string.IsNullOrWhiteSpace(response.ReplyText))
            {
                if (interactionPopupUI != null)
                    interactionPopupUI.ShowDialogue(response.ReplyText);
                else
                    ReceiveDialogueFromNPC(new[] { response.ReplyText });

                if (DialogueManager.Instance != null)
                    yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);

                if (isInteractionActive)
                    PlayerMovement.Instance.canMove = false;
            }

            switch (response.NpcAction)
            {
                case NpcActionType.Talk:
                    if (!string.IsNullOrWhiteSpace(response.NpcActionText))
                    {
                        if (interactionPopupUI != null)
                            interactionPopupUI.ShowDialogue(response.NpcActionText);
                        else
                            ReceiveDialogueFromNPC(new[] { response.NpcActionText });

                        if (DialogueManager.Instance != null)
                            yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);

                        if (isInteractionActive)
                            PlayerMovement.Instance.canMove = false;
                    }
                    break;
                case NpcActionType.GiveItem:
                    ReceiveItemFromNPC(response.ItemToGive);
                    break;
                case NpcActionType.Hit:
                    ReceiveHitFromNPC();
                    break;
            }
        }

        isPlayerTurn = true;
        if (interactionPopupUI != null)
            interactionPopupUI.SetTurnState(true);
    }

    public bool GiveItemToNPC(ItemData item)
    {
        if (currentNPC == null || item == null)
            return false;

        currentNPC.ReceiveItem(item);
        return true;
    }

    public void HitNPC()
    {
        if (currentNPC == null)
            return;

        currentNPC.ReceiveHit();
    }

    public bool ReceiveItemFromNPC(ItemData item)
    {
        if (item == null || inventory == null)
            return false;

        inventory.AddItem(item);
        Debug.Log("Player received item from NPC: " + item.itemName);
        return true;
    }

    public void ReceiveHitFromNPC()
    {
        Debug.Log("Player was hit.");
    }

    public void ReceiveDialogueFromNPC(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        DialogueManager.Instance.ShowDialogue(lines);
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
            if (isInteractionActive)
                EndInteraction();
            currentNPC = null;
        }
    }
}