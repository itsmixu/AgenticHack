using System.Collections;
using UnityEngine;

public class NPC : MonoBehaviour
{
    public static string sacrifice;

    [Header("References")]
    [SerializeField] private GameObject exclamation;
    [SerializeField] private ItemData flower;
    [SerializeField] private CharacterSheet charSheet;
    [SerializeField] private GameObject graveSpot;

    [Header("Dialogue")]
    [SerializeField] private bool isDaisy = false;
    [TextArea]
    [SerializeField] private string[] firstDialogue;
    [SerializeField] private string[] flowerDialogue;

    [Header("Player gives Item")]
    [SerializeField] private ItemData wantedItem;
    [TextArea]
    [SerializeField] private string[] correctItemDialogue;
    [TextArea]
    [SerializeField] private string[] wrongItemDialogue;

    [Header("NPC drops Item after dialogue")]
    [SerializeField] private bool requireCorrectItem = false;
    [SerializeField] private bool dropAlways = false;
    [SerializeField] private GameObject itemDrop = null;

    private Animator animator;

    private void Awake()
    {
        if (itemDrop != null)
        {
            itemDrop.SetActive(false);
        }
        animator = GetComponent<Animator>();
    }

    [HideInInspector] public bool playerNearby = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        exclamation.SetActive(true);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        exclamation.SetActive(false);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Input.GetKey(KeyCode.E) && DialogueManager.Instance.isDialogueActive == false)
        {
            Interact();
            CharacterSheetList.Instance.AddToList(charSheet);
        }
    }

    private void Interact()
    {
        //Basic NPCs
        DialogueManager.Instance.ShowDialogue(firstDialogue);
        if (itemDrop != null && dropAlways) StartCoroutine(DropItem());
    }

    public void ReceiveItem(ItemData item)
    {
        if (item == flower)
        {
            AudioManager.Instance.PlayMusic("Sacrifice");
            Instantiate(gameObject, transform.position, transform.rotation); //copy itself
            transform. position = graveSpot.transform.position; // move itself to the graveyard
            sacrifice = gameObject.name;
            DialogueManager.Instance.ShowDialogue(flowerDialogue);
        }
        else
        {
            if (item == wantedItem)
            {
                DialogueManager.Instance.ShowDialogue(correctItemDialogue);
                if (isDaisy)
                {
                    animator.Play("DaisyKazoo");
                    AudioManager.Instance.PlaySFX("Kazoo");
                }

                if (itemDrop != null && requireCorrectItem == true)
                {
                    StartCoroutine(DropItem());
                }
            }
            else
            {
                DialogueManager.Instance.ShowDialogue(wrongItemDialogue);
            }

            if (itemDrop != null && requireCorrectItem == false)
            {
                StartCoroutine(DropItem());
            }
        }
    }

    private IEnumerator DropItem()
    {
        yield return new WaitUntil(() => DialogueManager.Instance.isDialogueActive == false);
        itemDrop.SetActive(true);
    }
}

