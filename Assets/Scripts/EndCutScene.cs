using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class EndCutScene : MonoBehaviour
{
    [HideInInspector]
    public int endNumber;

    [Header("References")]
    public Inventory inventory;
    public ItemData flower;
    public GameObject phantom;
    public GameObject player;
    public Animator daisyAnim;
    public GameObject choiceUI;
    public GameObject inventoryUI;
    public GameObject charSheetUI;
    public GameObject charSheetPopupUI;
    public GameObject exclamation;

    public CinemachineCamera graveyardCam;

    [Header("End Screen Texts")]
    [TextArea]
    public string endText1;
    [TextArea]
    public string endText2;
    [TextArea]
    public string endTextStay;
    [TextArea]
    public string endTextLeave;

    [Header("Dialogue")]
    public string[] end1dialogue;
    public string[] end2dialogue;
    public string[] end3dialogue;

    public string[] end3StayDialogue1;
    public string[] end3StayDialogue2;
    public string[] end3StayDialogue3;
    
    public string[] end3LeaveDialogue;

    private bool started = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        exclamation.SetActive(true);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!started && Input.GetKey(KeyCode.E))
        {
            started = true;
            StartCoroutine(StartEndCutscene());
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        exclamation.SetActive(false);
    }

    private IEnumerator StartEndCutscene()
    {
        PlayerMovement.Instance.canMove = false;
        ScreenFlash.Instance.FadeIn(Color.black, 2f);
        AudioManager.Instance.PlayMusic("Ghost");
        yield return new WaitForSeconds(3f);
        graveyardCam.enabled = true;
        inventoryUI.SetActive(false);
        charSheetUI.SetActive(false);
        charSheetPopupUI.SetActive(false);
        ScreenFlash.Instance.FadeOut(Color.black, 2f);

        if (inventory.items.Contains(flower))
        {
            endNumber = 1;
        }
        else if (NPC.sacrifice != "Daisy")
        {
            endNumber = 2;
        }
        else if (NPC.sacrifice == "Daisy")
        {
            endNumber = 3;
            phantom.SetActive(false);
        }
        Debug.Log("Ending is " + endNumber);


        switch (endNumber)
        {
            case 1:
                {
                    StartCoroutine(Ending1());
                    break;
                }
            case 2:    // END 2, player brings any NPC other than DAISY
                {
                    StartCoroutine(Ending2());
                    break;
                }
            case 3:    // END 3, player brings DAISY
                {
                    StartCoroutine(Ending3());
                    break;
                }
        }
    }


    IEnumerator Ending1() // END 1, player has flower in inv
    {
        DialogueManager.Instance.ShowDialogue(end1dialogue);
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);

        ScreenFlash.Instance.FadeIn(Color.black, 3f);
        yield return new WaitForSeconds(3f);

        EndScreen.Instance.ShowEndScreen(endText1, false);
        ScreenFlash.Instance.FadeOut(Color.black, 3f);
    }

    IEnumerator Ending2() // END 2, player brings any NPC other than DAISY
    {
        DialogueManager.Instance.ShowDialogue(end2dialogue);
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);
        ScreenFlash.Instance.FadeIn(Color.white, 0.2f);
        yield return new WaitForSeconds(3f);
        EndScreen.Instance.ShowEndScreen(endText2, false);
        ScreenFlash.Instance.FadeOut(Color.white, 3f);
    }

    IEnumerator Ending3() // END 3, player brings DAISY
    {
        DialogueManager.Instance.ShowDialogue(end3dialogue);
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);
        choiceUI.SetActive(true);

        // Wait until 1 or 2 pressed
        while (!Input.GetKeyDown(KeyCode.Alpha1) && !Input.GetKeyDown(KeyCode.Alpha2))
        {
            yield return null; // wait for next frame
        }

        choiceUI.SetActive(false);

        if (Input.GetKeyDown(KeyCode.Alpha1)) // Stay
        {
            Debug.Log("Staying");
            DialogueManager.Instance.ShowDialogue(end3StayDialogue1);
            yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);
            yield return new WaitForSeconds(1.5f);

            phantom.SetActive(true);
            DialogueManager.Instance.ShowDialogue(end3StayDialogue2);
            yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);

            yield return new WaitForSeconds(2f);
            AudioManager.Instance.PlaySFX("Pop");

            ScreenFlash.Instance.FadeIn(Color.white, 0.2f);
            ScreenFlash.Instance.FadeOut(Color.white, 2f);
            daisyAnim.Play("DaisyPop");

            yield return new WaitForSeconds(4f);

            /*
            DialogueManager.Instance.ShowDialogue(end3StayDialogue3);
            yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);
            */

            ScreenFlash.Instance.FadeIn(Color.black, 3f);
            yield return new WaitForSeconds(3f);
            EndScreen.Instance.ShowEndScreen(endTextStay, true);
            ScreenFlash.Instance.FadeOut(Color.black, 4f);

        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) // Leave
        {
            Debug.Log("Leaving");
            DialogueManager.Instance.ShowDialogue(end3LeaveDialogue);
            yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);

            ScreenFlash.Instance.FadeIn(Color.black, 3f);
            yield return new WaitForSeconds(3f);

            EndScreen.Instance.ShowEndScreen(endTextLeave, false);
            ScreenFlash.Instance.FadeOut(Color.black, 3f);
        }
    }
}