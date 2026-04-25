using TMPro;
using UnityEngine;

public class InteractionPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void Show()
    {
        if (panel != null)
            panel.SetActive(true);

        if (inputField != null)
            inputField.text = string.Empty;

        SetStatus("Your turn.");
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    public void SetTurnState(bool isPlayerTurn)
    {
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

        string itemName = inputField != null ? inputField.text : string.Empty;
        PlayerInteraction.Instance.SubmitGiveItem(itemName);
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
}
