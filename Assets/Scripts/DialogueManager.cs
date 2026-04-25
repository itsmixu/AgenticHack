using TMPro;
using UnityEngine;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject dialoguePanel;

    private string currentLine;
    public bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool canAdvance = false;

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
        // Singleton pattern for easy access
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        EnsureReferences();
    }

    private void StartTypingLine(string line)
    {
        if (dialogueLines == null || dialogueLines.Length == 0 || currentLineIndex < 0 || currentLineIndex >= dialogueLines.Length)
        {
            Debug.LogWarning("DialogueManager: No dialogue lines to display.");
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line));
    }

    private IEnumerator TypeText(string line)
    {
        isTyping = true;

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
        isTyping = false;
        typingCoroutine = null; // typing finished
    }

    public void ShowDialogue(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        currentLine = line;
        isDialogueActive = true;
        canAdvance = false;
        dialoguePanel.SetActive(true);
        PlayerMovement.Instance.canMove = false;
        StartTypingLine(currentLine);
        StartCoroutine(EnableAdvanceNextFrame());
    }

    // Backward compatibility for existing callers; only first entry is shown.
    public void ShowDialogue(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        ShowDialogue(lines[0]);
    }

    private IEnumerator EnableAdvanceNextFrame()
    {
        yield return null; // Wait one frame before allowing input
        canAdvance = true;
    }

    private void Update()
    {
        if (isDialogueActive && canAdvance && Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                if (typingCoroutine != null)
                    StopCoroutine(typingCoroutine);

                dialogueText.text = currentLine;
                isTyping = false;
                typingCoroutine = null;
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
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        isDialogueActive = false;
        currentLine = null;
        isTyping = false;
        canAdvance = false;
        typingCoroutine = null;
        Debug.Log("[DialogueManager] EndDialogue");
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.canMove = true;
    }
}

