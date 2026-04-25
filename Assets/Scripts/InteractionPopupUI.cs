using TMPro;
using UnityEngine;

public class InteractionPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject actionButtonsParent;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI statusText;

    /// <summary>Player chat / NPC reply box (same UI the player types into).</summary>
    public TMP_InputField NpcChatInput => inputField;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);

        if (actionButtonsParent != null)
            actionButtonsParent.SetActive(true);

        if (inputField != null)
            inputField.text = string.Empty;

        SetStatus("Your turn.");
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);

        if (actionButtonsParent != null)
            actionButtonsParent.SetActive(false);
    }

    public void SetTurnState(bool isPlayerTurn)
    {
        if (actionButtonsParent != null)
            actionButtonsParent.SetActive(isPlayerTurn);

        if (inputField != null)
        {
            // Keep chat box fully enabled visually and functionally during NPC turn.
            inputField.interactable = true;
            inputField.readOnly = false;
        }

        SetStatus(isPlayerTurn ? "Your turn." : "NPC is thinking...");
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void ShowDialogue(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(new[] { message });
        }
        else
        {
            SetStatus(message);
        }
    }

    public void OnTalkPressed()
    {
        if (PlayerInteraction.Instance == null)
            return;

        string message = inputField != null ? inputField.text : string.Empty;
        PlayerInteraction.Instance.SubmitTalk(message);
    }

    public void OnGiveItemPressed()
    {
        if (PlayerInteraction.Instance == null)
            return;

        PlayerInteraction.Instance.BeginGiveItemSelection();
    }

    public void OnHitPressed()
    {
        if (PlayerInteraction.Instance == null)
            return;

        PlayerInteraction.Instance.SubmitHit();
    }

    public void OnEndInteractionPressed()
    {
        if (PlayerInteraction.Instance == null)
            return;

        PlayerInteraction.Instance.EndInteraction();
    }

    public void BeginGiveItemSelectionPrompt()
    {
        if (actionButtonsParent != null)
            actionButtonsParent.SetActive(false);

        if (inputField != null)
        {
            inputField.interactable = false;
            inputField.text = "Press 1-5 to choose an inventory slot.";
            inputField.DeactivateInputField();
        }

        SetStatus("Choose item slot (1-5).");
    }

    public void EndGiveItemSelectionPrompt()
    {
        if (actionButtonsParent != null)
            actionButtonsParent.SetActive(true);

        if (inputField != null)
        {
            inputField.interactable = true;
            inputField.text = string.Empty;
        }
    }
}
