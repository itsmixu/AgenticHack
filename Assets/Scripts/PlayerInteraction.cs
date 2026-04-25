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

        if (useBackendAI)
            StartCoroutine(CheckBackendHealth());
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

    public void SubmitHit()
    {
        if (!CanAct())
            return;

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

        bool eventOk = false;
        yield return StartCoroutine(PostEvent(playerId, npcId, action, ok => eventOk = ok));

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
            yield return request.SendWebRequest();
            bool ok = request.result == UnityWebRequest.Result.Success;
            if (!ok)
                Debug.LogWarning("Event request failed: " + request.error);
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
            yield return request.SendWebRequest();
            bool ok = request.result == UnityWebRequest.Result.Success;
            string text = ok ? request.downloadHandler.text : string.Empty;
            if (!ok)
                Debug.LogWarning("Response request failed: " + request.error);
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
            yield return request.SendWebRequest();
            bool ok = request.result == UnityWebRequest.Result.Success;
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

    private bool CanAct()
    {
        return isInteractionActive && isPlayerTurn && currentNPC != null;
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