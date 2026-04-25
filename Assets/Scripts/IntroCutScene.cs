using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class IntroCutScene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera graveyardCam;
    [SerializeField] private GameObject ghost;
    [SerializeField] private GameObject InventoryUI;
    [SerializeField] private GameObject tutorialPopup;
    [Header("Dialogue")]
    [SerializeField] private string[] startDialogue;
    [SerializeField] private string[] ghostDialogue;
    void Start()
    {
        ghost.SetActive(false);
        ScreenFlash.Instance.FadeOut(Color.black, 3);
        AudioManager.Instance.PlayMusic("Sad");
        StartCoroutine(Intro());
    }

    IEnumerator Intro()
    {
        yield return new WaitForSeconds(2f);
        DialogueManager.Instance.ShowDialogue(startDialogue);
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);

        yield return new WaitForSeconds(2f);
        
        ghost.SetActive(true);
        AudioManager.Instance.PlayMusic("Ghost");
        AudioManager.Instance.PlaySFX("Kazoo");
        yield return new WaitForSeconds(1f);
        DialogueManager.Instance.ShowDialogue(ghostDialogue);
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);


        yield return new WaitForSeconds(1f);
        ScreenFlash.Instance.FadeIn(Color.black, 2f);
        yield return new WaitForSeconds(2f);
        InventoryUI.SetActive(true);
        graveyardCam.enabled = false;
        tutorialPopup.SetActive(true);
        AudioManager.Instance.PlayMusic("Background");
        yield return new WaitForSeconds(1f);
        ScreenFlash.Instance.FadeOut(Color.black, 1f);
    }
}
