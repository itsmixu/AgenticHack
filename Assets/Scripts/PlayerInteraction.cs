using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance;

    public NPC currentNPC;
    [SerializeField] private InteractionPopupUI interactionPopupUI;

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

        float requestStartTime = Time.realtimeSinceStartup;
        // API INTEGRATION POINT:
        // Replace the local ProcessPlayerAction call below with your async API request.
        // Send: NPC identity + PlayerActionType.Talk + message.
        // Map API JSON response into NpcInteractionResponse, then pass it to ProcessNpcTurn.
        NpcInteractionResponse response = currentNPC.ProcessPlayerAction(PlayerActionType.Talk, message, null);
        StartCoroutine(ProcessNpcTurn(response, requestStartTime));
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
        float requestStartTime = Time.realtimeSinceStartup;
        // API INTEGRATION POINT:
        // Replace the local ProcessPlayerAction call below with your async API request.
        // Send: NPC identity + PlayerActionType.GiveItem + item payload (item name/id).
        // Map API JSON response into NpcInteractionResponse, then pass it to ProcessNpcTurn.
        NpcInteractionResponse response = currentNPC.ProcessPlayerAction(PlayerActionType.GiveItem, null, itemToGive);
        StartCoroutine(ProcessNpcTurn(response, requestStartTime));
    }

    public void SubmitHit()
    {
        if (!CanAct())
            return;

        float requestStartTime = Time.realtimeSinceStartup;
        // API INTEGRATION POINT:
        // Replace the local ProcessPlayerAction call below with your async API request.
        // Send: NPC identity + PlayerActionType.Hit.
        // Map API JSON response into NpcInteractionResponse, then pass it to ProcessNpcTurn.
        NpcInteractionResponse response = currentNPC.ProcessPlayerAction(PlayerActionType.Hit, null, null);
        StartCoroutine(ProcessNpcTurn(response, requestStartTime));
    }

    private bool CanAct()
    {
        return isInteractionActive && isPlayerTurn && currentNPC != null;
    }

    private System.Collections.IEnumerator ShowNpcLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            yield break;

        if (interactionPopupUI != null)
            interactionPopupUI.ShowDialogue(line);
        else
            ReceiveDialogueFromNPC(new[] { line });

        if (ElevenLabsTTS.Instance != null && currentNPC != null)
            yield return StartCoroutine(ElevenLabsTTS.Instance.Speak(line, currentNPC.VoiceId));

        if (DialogueManager.Instance != null)
            yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);

        if (isInteractionActive)
            PlayerMovement.Instance.canMove = false;
    }

    private System.Collections.IEnumerator ProcessNpcTurn(NpcInteractionResponse response, float requestStartTime)
    {
        isPlayerTurn = false;
        if (interactionPopupUI != null)
            interactionPopupUI.SetTurnState(false);

        if (response != null)
        {
            if (!string.IsNullOrWhiteSpace(response.ReplyText))
                yield return StartCoroutine(ShowNpcLine(response.ReplyText));

            switch (response.NpcAction)
            {
                case NpcActionType.Talk:
                    if (!string.IsNullOrWhiteSpace(response.NpcActionText))
                        yield return StartCoroutine(ShowNpcLine(response.NpcActionText));
                    break;
                case NpcActionType.GiveItem:
                    // API should populate response.ItemToGive when NPC decides to give an item.
                    ReceiveItemFromNPC(response.ItemToGive);
                    break;
                case NpcActionType.Hit:
                    ReceiveHitFromNPC();
                    break;
            }
        }

        float responseMs = (Time.realtimeSinceStartup - requestStartTime) * 1000f;
        isPlayerTurn = true;
        if (interactionPopupUI != null)
            interactionPopupUI.SetStatus($"API responded in {responseMs:0} ms. Your turn.");
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