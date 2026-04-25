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

    private void Awake()
    {
        // Singleton pattern for easy access
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void StartTypingLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(dialogueLines[currentLineIndex]));
    }

    private IEnumerator TypeText(string line)
    {
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.05f);
        }
        typingCoroutine = null; // typing finished
    }
    private bool canAdvance = false;

    public void ShowDialogue(string[] lines)
    {
        dialogueLines = lines;
        currentLineIndex = 0;
        isDialogueActive = true;
        canAdvance = false;
        dialoguePanel.SetActive(true);
        PlayerMovement.Instance.canMove = false;
        StartTypingLine();
        StartCoroutine(EnableAdvanceNextFrame());

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
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
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
    }

    private IEnumerator EndDialogue()
    {
        yield return new WaitForSeconds(0.1f);
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        typingCoroutine = null;
        PlayerMovement.Instance.canMove = true;
    }
}

