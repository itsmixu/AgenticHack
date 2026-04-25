using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterSheet", menuName = "Inventory/Character Sheet")]
public class CharacterSheet : ScriptableObject
{
    public Sprite name;
    public Sprite image;
    public string note;
}
