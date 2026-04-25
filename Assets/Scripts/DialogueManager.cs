using TMPro;
using UnityEngine;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject dialoguePanel;

    private string[] dialogueLines;
    private int currentLineIndex = 0;
    public bool isDialogueActive = false;
    private Coroutine typingCoroutine;

    /// <summary>When set, NPC dialogue is typed into the interaction chat field instead of a separate label.</summary>
    private TMP_InputField boundNpcChatInput;

    private void EnsureReferences()
    {
        if (dialoguePanel == null)
        {
            dialoguePanel = gameObject;
            Debug.LogWarning("DialogueManager: dialoguePanel was null, defaulting to this GameObject.");
        }

        if (dialogueText == null)
        {
            dialogueText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (dialogueText != null)
                Debug.LogWarning("DialogueManager: Auto-bound dialogueText from child TextMeshProUGUI.");
        }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        EnsureReferences();
    }

    private void StartTypingLine()
    {
        if (dialogueLines == null || dialogueLines.Length == 0 || currentLineIndex < 0 || currentLineIndex >= dialogueLines.Length)
        {
            Debug.LogWarning("DialogueManager: No dialogue lines to display.");
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(dialogueLines[currentLineIndex]));
    }

    private IEnumerator TypeText(string line)
    {
        if (boundNpcChatInput != null)
        {
            boundNpcChatInput.text = string.Empty;
            for (int i = 0; i < line.Length; i++)
            {
                boundNpcChatInput.text = line.Substring(0, i + 1);
                yield return new WaitForSecondsRealtime(0.05f);
            }
            typingCoroutine = null;
            yield break;
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("DialogueManager: dialogueText reference is missing.");
            yield break;
        }

        if (!dialogueText.gameObject.activeSelf)
            dialogueText.gameObject.SetActive(true);
        dialogueText.enabled = true;

        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(0.05f);
        }
        typingCoroutine = null;
    }

    private bool canAdvance = false;

    public void ShowDialogue(string[] lines)
    {
        EnsureReferences();

        boundNpcChatInput = null;
        if (PlayerInteraction.Instance != null && PlayerInteraction.Instance.NpcChatInput != null)
            boundNpcChatInput = PlayerInteraction.Instance.NpcChatInput;

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("DialogueManager: ShowDialogue called with empty lines.");
            return;
        }

        bool useChatInput = boundNpcChatInput != null;
        if (!useChatInput && (dialoguePanel == null || dialogueText == null))
        {
            Debug.LogWarning("DialogueManager: dialoguePanel or dialogueText is not assigned in inspector.");
            return;
        }

        dialogueLines = lines;
        currentLineIndex = 0;
        isDialogueActive = true;
        canAdvance = false;

        if (useChatInput)
        {
            boundNpcChatInput.interactable = true;
            if (!boundNpcChatInput.gameObject.activeInHierarchy)
                boundNpcChatInput.gameObject.SetActive(true);
            boundNpcChatInput.text = lines[0];
            Debug.Log($"[DialogueManager] ShowDialogue (chat input) lines={lines.Length} first='{lines[0]}' input={boundNpcChatInput.name}");
            //if (dialoguePanel != null && dialoguePanel != boundNpcChatInput.gameObject)
                //dialoguePanel.SetActive(false);
        }
        else
        {
            dialoguePanel.SetActive(true);
            Debug.Log($"[DialogueManager] ShowDialogue lines={lines.Length} first='{lines[0]}' panel={dialoguePanel.name} text={dialogueText.name} activeInHierarchy={dialoguePanel.activeInHierarchy}");
            if (!dialogueText.gameObject.activeSelf)
                dialogueText.gameObject.SetActive(true);
            dialogueText.enabled = true;
            dialogueText.alpha = 1f;
            Color c = dialogueText.color;
            dialogueText.color = new Color(c.r, c.g, c.b, 1f);
            dialogueText.text = lines[0];
        }

        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.canMove = false;

        StartTypingLine();
        StartCoroutine(EnableAdvanceNextFrame());
    }

    private IEnumerator EnableAdvanceNextFrame()
    {
        yield return null;
        canAdvance = true;
    }

    private void Update()
    {
        if (!isDialogueActive || !canAdvance || !Input.GetKeyDown(KeyCode.E))
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            if (boundNpcChatInput != null)
                boundNpcChatInput.text = dialogueLines[currentLineIndex];
            else if (dialogueText != null)
                dialogueText.text = dialogueLines[currentLineIndex];
            typingCoroutine = null;
        }
        else
        {
            currentLineIndex++;
            if (currentLineIndex < dialogueLines.Length)
            {
                StartTypingLine();
            }
            else
            {
                StartCoroutine(EndDialogue());
            }
        }
    }

    private IEnumerator EndDialogue()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        if (boundNpcChatInput != null)
        {
            boundNpcChatInput.text = string.Empty;
            boundNpcChatInput = null;
        }
        else if (dialoguePanel != null)
        {
            //dialoguePanel.SetActive(false);
        }

        isDialogueActive = false;
        typingCoroutine = null;
        Debug.Log("[DialogueManager] EndDialogue");
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.canMove = true;
    }
}
