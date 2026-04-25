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

    private void Awake()
    {
        // Singleton pattern for easy access
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void StartTypingLine(string line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line));
    }

    private IEnumerator TypeText(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.05f);
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
        yield return new WaitForSeconds(0.1f);
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        currentLine = null;
        isTyping = false;
        canAdvance = false;
        typingCoroutine = null;
        PlayerMovement.Instance.canMove = true;
    }
}

