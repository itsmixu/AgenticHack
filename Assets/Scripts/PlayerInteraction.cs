using UnityEngine;
using UnityEngine.Networking;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance;

    public NPC currentNPC;
    [SerializeField] private InteractionPopupUI interactionPopupUI;
    [Header("Backend AI")]
    [SerializeField] private bool useBackendAI = true;
    [SerializeField] private string backendBaseUrl = "http://localhost:8000";
    [SerializeField] private float requestTimeoutSeconds = 8f;
    [SerializeField] private string playerId = "p1";

    private Inventory inventory;
    private bool isInteractionActive;
    private bool isPlayerTurn;
    private bool isAwaitingGiveItemSlot;

    public bool IsInteractionActive => isInteractionActive;
    public bool IsPlayerTurn => isPlayerTurn;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (string.IsNullOrWhiteSpace(playerId))
        {
            playerId = PlayerPrefs.GetString("player_id", string.Empty);
            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString("player_id", playerId);
                PlayerPrefs.Save();
            }
        }

        inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        if (currentNPC == null || IsDialogueActive())
            return;

        if (isInteractionActive && isPlayerTurn && isAwaitingGiveItemSlot)
        {
            HandleGiveItemSlotInput();
            return;
        }

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
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.canMove = false;

        if (interactionPopupUI != null)
        {
            interactionPopupUI.Show();
            interactionPopupUI.SetTurnState(true);
        }

        if (useBackendAI)
            StartCoroutine(CheckBackendHealth());

        Debug.Log($"[PlayerInteraction] BeginInteraction npc={(currentNPC != null ? currentNPC.NpcId : "null")} backendAI={useBackendAI}");
    }

    public void EndInteraction()
    {
        if (!isInteractionActive)
            return;

        isInteractionActive = false;
        isPlayerTurn = false;
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.canMove = true;

        if (interactionPopupUI != null)
        {
            interactionPopupUI.Hide();
        }

        isAwaitingGiveItemSlot = false;
    }

    public void SubmitTalk(string message)
    {
        if (!CanAct())
            return;

        Debug.Log($"[PlayerInteraction] SubmitTalk message='{message}'");

        float requestStartTime = Time.realtimeSinceStartup;
        if (useBackendAI)
        {
            StartCoroutine(HandleBackendAction(PlayerActionType.Talk, message, null, requestStartTime));
            return;
        }

        NpcInteractionResponse localResponse = currentNPC.ProcessPlayerAction(PlayerActionType.Talk, message, null);
        StartCoroutine(ProcessNpcTurn(localResponse, requestStartTime));
    }

    public void SubmitGiveItem(string itemName)
    {
        if (!CanAct())
            return;

        Debug.Log($"[PlayerInteraction] SubmitGiveItem itemName='{itemName}'");

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

        float requestStartTime = Time.realtimeSinceStartup;
        if (useBackendAI)
        {
            StartCoroutine(HandleBackendAction(PlayerActionType.GiveItem, null, itemToGive, requestStartTime));
            return;
        }

        inventory.RemoveItem(itemToGive);
        NpcInteractionResponse localResponse = currentNPC.ProcessPlayerAction(PlayerActionType.GiveItem, null, itemToGive);
        StartCoroutine(ProcessNpcTurn(localResponse, requestStartTime));
    }

    public void BeginGiveItemSelection()
    {
        if (!CanAct())
            return;

        isAwaitingGiveItemSlot = true;
        if (interactionPopupUI != null)
            interactionPopupUI.BeginGiveItemSelectionPrompt();
    }

    public void SubmitHit()
    {
        if (!CanAct())
            return;

        Debug.Log("[PlayerInteraction] SubmitHit");

        float requestStartTime = Time.realtimeSinceStartup;
        if (useBackendAI)
        {
            StartCoroutine(HandleBackendAction(PlayerActionType.Hit, null, null, requestStartTime));
            return;
        }

        NpcInteractionResponse localResponse = currentNPC.ProcessPlayerAction(PlayerActionType.Hit, null, null);
        StartCoroutine(ProcessNpcTurn(localResponse, requestStartTime));
    }

    private System.Collections.IEnumerator HandleBackendAction(PlayerActionType actionType, string playerMessage, ItemData givenItem, float requestStartTime)
    {
        isPlayerTurn = false;
        if (interactionPopupUI != null)
            interactionPopupUI.SetTurnState(false);

        string npcId = currentNPC != null ? currentNPC.NpcId : string.Empty;
        string action = MapActionForBackend(actionType);
        string message = BuildMessageForResponse(actionType, playerMessage, givenItem);

        Debug.Log($"[PlayerInteraction] HandleBackendAction start actionType={actionType} mappedAction={action} playerId={playerId} npcId={npcId} message='{message}'");

        bool eventOk = false;
        yield return StartCoroutine(PostEvent(playerId, npcId, action, ok => eventOk = ok));

        Debug.Log($"[PlayerInteraction] PostEvent completed ok={eventOk}");

        if (!eventOk)
        {
            if (interactionPopupUI != null)
                interactionPopupUI.SetStatus("Could not sync event. Try again.");
            isPlayerTurn = true;
            if (interactionPopupUI != null)
                interactionPopupUI.SetTurnState(true);
            yield break;
        }

        if (actionType == PlayerActionType.GiveItem && givenItem != null && inventory != null)
            inventory.RemoveItem(givenItem);

        string npcReply = string.Empty;
        bool responseOk = false;
        yield return StartCoroutine(PostResponse(playerId, npcId, message, (ok, text) =>
        {
            responseOk = ok;
            npcReply = text;
        }));

        Debug.Log($"[PlayerInteraction] PostResponse completed ok={responseOk} reply='{npcReply}'");

        NpcInteractionResponse response;
        if (responseOk)
        {
            response = new NpcInteractionResponse
            {
                ReplyText = string.IsNullOrWhiteSpace(npcReply) ? "I have nothing to say." : npcReply,
                NpcAction = NpcActionType.None
            };
        }
        else
        {
            response = new NpcInteractionResponse
            {
                ReplyText = "I cannot speak clearly right now.",
                NpcAction = NpcActionType.None
            };
        }

        yield return StartCoroutine(ProcessNpcTurn(response, requestStartTime));
        Debug.Log("[PlayerInteraction] HandleBackendAction end");
    }

    private System.Collections.IEnumerator PostEvent(string currentPlayerId, string npcId, string action, System.Action<bool> onDone)
    {
        EventRequest payload = new EventRequest
        {
            player_id = currentPlayerId,
            npc_id = npcId,
            action = action
        };

        string json = JsonUtility.ToJson(payload);
        using (UnityWebRequest request = BuildJsonPost(BuildUrl("/event"), json))
        {
            request.timeout = Mathf.CeilToInt(requestTimeoutSeconds);
            Debug.Log($"[PlayerInteraction] POST /event url={request.url} body={json}");
            yield return request.SendWebRequest();
            bool ok = request.result == UnityWebRequest.Result.Success;
            if (!ok)
                Debug.LogWarning("Event request failed: " + request.error);
            else
                Debug.Log($"[PlayerInteraction] /event response={request.downloadHandler.text}");
            onDone?.Invoke(ok);
        }
    }

    private System.Collections.IEnumerator PostResponse(string currentPlayerId, string npcId, string message, System.Action<bool, string> onDone)
    {
        ResponseRequest payload = new ResponseRequest
        {
            player_id = currentPlayerId,
            npc_id = npcId,
            message = message
        };

        string json = JsonUtility.ToJson(payload);
        using (UnityWebRequest request = BuildJsonPost(BuildUrl("/response"), json))
        {
            request.timeout = Mathf.CeilToInt(requestTimeoutSeconds);
            Debug.Log($"[PlayerInteraction] POST /response url={request.url} body={json}");
            yield return request.SendWebRequest();
            bool ok = request.result == UnityWebRequest.Result.Success;
            string text = ok ? NormalizeServerText(request.downloadHandler.text) : string.Empty;
            if (!ok)
                Debug.LogWarning("Response request failed: " + request.error);
            else
                Debug.Log($"[PlayerInteraction] /response raw={request.downloadHandler.text} normalized='{text}'");
            onDone?.Invoke(ok, text);
        }
    }

    private static UnityWebRequest BuildJsonPost(string url, string jsonBody)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private string BuildUrl(string path)
    {
        string baseUrl = string.IsNullOrWhiteSpace(backendBaseUrl) ? "http://localhost:8000" : backendBaseUrl.Trim();
        return baseUrl.TrimEnd('/') + path;
    }

    private System.Collections.IEnumerator CheckBackendHealth()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(BuildUrl("/health")))
        {
            request.timeout = Mathf.CeilToInt(requestTimeoutSeconds);
            Debug.Log($"[PlayerInteraction] GET /health url={request.url}");
            yield return request.SendWebRequest();
            bool ok = request.result == UnityWebRequest.Result.Success;
            Debug.Log($"[PlayerInteraction] /health ok={ok} response={(ok ? request.downloadHandler.text : request.error)}");
            if (interactionPopupUI != null && isInteractionActive)
            {
                if (ok)
                    interactionPopupUI.SetStatus("Connected to AI backend. Your turn.");
                else
                    interactionPopupUI.SetStatus("Backend offline. Check server on localhost:8000.");
            }
        }
    }

    private static string MapActionForBackend(PlayerActionType actionType)
    {
        switch (actionType)
        {
            case PlayerActionType.Hit:
                return "ATTACK";
            case PlayerActionType.GiveItem:
                return "HELP";
            case PlayerActionType.Talk:
            default:
                return "TALK";
        }
    }

    private static string BuildMessageForResponse(PlayerActionType actionType, string playerMessage, ItemData givenItem)
    {
        switch (actionType)
        {
            case PlayerActionType.Hit:
                return "Do you remember what I did to you?";
            case PlayerActionType.GiveItem:
                return givenItem == null ? "Do you remember me?" : "I gave you " + givenItem.itemName + ". Do you remember me?";
            case PlayerActionType.Talk:
            default:
                return string.IsNullOrWhiteSpace(playerMessage) ? "hello" : playerMessage;
        }
    }

    private static string NormalizeServerText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string cleaned = text.Trim();
        if (cleaned.Length >= 2 && cleaned[0] == '"' && cleaned[cleaned.Length - 1] == '"')
            cleaned = cleaned.Substring(1, cleaned.Length - 2);

        return cleaned.Trim();
    }

    private static bool IsDialogueActive()
    {
        return DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;
    }

    private bool CanAct()
    {
        return isInteractionActive && isPlayerTurn && currentNPC != null;
    }

    private void HandleGiveItemSlotInput()
    {
        int slotIndex = -1;

        if (Input.GetKeyDown(KeyCode.Alpha1)) slotIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) slotIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) slotIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) slotIndex = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) slotIndex = 4;

        if (slotIndex < 0)
            return;

        if (inventory == null)
        {
            if (interactionPopupUI != null)
                interactionPopupUI.SetStatus("Player inventory not found.");
            return;
        }

        if (slotIndex >= inventory.items.Count || inventory.items[slotIndex] == null)
        {
            if (interactionPopupUI != null)
                interactionPopupUI.SetStatus("No item in that slot. Press 1-5.");
            return;
        }

        ItemData itemToGive = inventory.items[slotIndex];
        inventory.RemoveItem(itemToGive);

        isAwaitingGiveItemSlot = false;
        if (interactionPopupUI != null)
            interactionPopupUI.EndGiveItemSelectionPrompt();

        float requestStartTime = Time.realtimeSinceStartup;
        if (useBackendAI)
        {
            StartCoroutine(HandleBackendAction(PlayerActionType.GiveItem, null, itemToGive, requestStartTime));
            return;
        }

        NpcInteractionResponse response = currentNPC.ProcessPlayerAction(PlayerActionType.GiveItem, null, itemToGive);
        StartCoroutine(ProcessNpcTurn(response, requestStartTime));
    }

    [System.Serializable]
    private class EventRequest
    {
        public string player_id;
        public string npc_id;
        public string action;
    }

    [System.Serializable]
    private class ResponseRequest
    {
        public string player_id;
        public string npc_id;
        public string message;
    }

    private System.Collections.IEnumerator ShowNpcLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            yield break;

        Debug.Log($"[PlayerInteraction] ShowNpcLine '{line}'");

        ReceiveDialogueFromNPC(new[] { line });

        if (ElevenLabsTTS.Instance != null && currentNPC != null)
            yield return StartCoroutine(ElevenLabsTTS.Instance.Speak(line, currentNPC.VoiceId));

        if (DialogueManager.Instance != null)
            yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);

        if (isInteractionActive && PlayerMovement.Instance != null)
            PlayerMovement.Instance.canMove = false;
    }

    private System.Collections.IEnumerator ProcessNpcTurn(NpcInteractionResponse response, float requestStartTime)
    {
        isPlayerTurn = false;
        isAwaitingGiveItemSlot = false;
        if (interactionPopupUI != null)
        {
            interactionPopupUI.EndGiveItemSelectionPrompt();
            interactionPopupUI.SetTurnState(false);
        }

        Debug.Log($"[PlayerInteraction] ProcessNpcTurn start hasResponse={(response != null)} reply='{(response != null ? response.ReplyText : "null")}'");

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
        Debug.Log($"[PlayerInteraction] ProcessNpcTurn end responseMs={responseMs:0}");
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

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(lines);
        }
        else if (Instance != null && Instance.interactionPopupUI != null)
        {
            Instance.interactionPopupUI.SetStatus(lines[0]);
        }
        else
        {
            Debug.LogWarning("DialogueManager missing; cannot display NPC dialogue panel.");
        }
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