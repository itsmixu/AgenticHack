using System.Collections;
using UnityEngine;

public class NPC : MonoBehaviour
{
    public static string sacrifice;

    [Header("References")]
    [SerializeField] private GameObject exclamation;
    [SerializeField] private ItemData flower;
    [SerializeField] private GameObject graveSpot;

    [Header("Dialogue")]
    [SerializeField] private bool isDaisy = false;
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

    [Header("NPC State")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHealth;

    private Animator animator;

    private void Awake()
    {
        if (itemDrop != null)
        {
            itemDrop.SetActive(false);
        }

        currentHealth = maxHealth;
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

    public void TalkToPlayer()
    {
        // Legacy entry point kept for compatibility.
        if (itemDrop != null && dropAlways) StartCoroutine(DropItem());
    }

    public NpcInteractionResponse ProcessPlayerAction(PlayerActionType actionType, string playerMessage, ItemData givenItem)
    {
        switch (actionType)
        {
            case PlayerActionType.Talk:
                return new NpcInteractionResponse
                {
                    ReplyText = string.IsNullOrWhiteSpace(playerMessage)
                        ? name + " is waiting for you to say something."
                        : name + " heard: " + playerMessage,
                    NpcAction = NpcActionType.None
                };

            case PlayerActionType.GiveItem:
                return new NpcInteractionResponse
                {
                    ReplyText = givenItem == null
                        ? name + " did not receive an item."
                        : name + " received " + givenItem.itemName + ".",
                    NpcAction = NpcActionType.None
                };

            case PlayerActionType.Hit:
                return new NpcInteractionResponse
                {
                    ReplyText = name + " was hit.",
                    NpcAction = NpcActionType.Hit
                };

            default:
                return new NpcInteractionResponse
                {
                    ReplyText = name + " did not understand the action.",
                    NpcAction = NpcActionType.None
                };
        }
    }

    public void SendDialogueToPlayer(string[] lines)
    {
        if (PlayerInteraction.Instance == null)
            return;

        PlayerInteraction.Instance.ReceiveDialogueFromNPC(lines);
    }

    public bool GiveItemToPlayer(ItemData item)
    {
        if (PlayerInteraction.Instance == null)
            return false;

        return PlayerInteraction.Instance.ReceiveItemFromNPC(item);
    }

    public void HitPlayer()
    {
        if (PlayerInteraction.Instance == null)
            return;

        PlayerInteraction.Instance.ReceiveHitFromNPC();
    }

    public void ReceiveHit()
    {
        Debug.Log(name + " was hit.");
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

