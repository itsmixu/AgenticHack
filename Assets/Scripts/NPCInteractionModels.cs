using UnityEngine;

public enum PlayerActionType
{
    Talk,
    GiveItem,
    Hit,
    EndInteraction
}

public enum NpcActionType
{
    None,
    Talk,
    GiveItem,
    Hit
}

public class NpcInteractionResponse
{
    public string ReplyText;
    public NpcActionType NpcAction;
    public string NpcActionText;
    public ItemData ItemToGive;
}
