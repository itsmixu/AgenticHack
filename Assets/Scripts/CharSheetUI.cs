using UnityEngine;
using UnityEngine.UI;

public class CharSheetUI : MonoBehaviour
{
    [SerializeField] private Image faceImage;
    [SerializeField] private GameObject image;

    private CharacterSheet charSheet;

    public void SetItem(CharacterSheet item)
    {
        faceImage.sprite = item.image;
        charSheet = item;

        image.SetActive(true);
    }

    public void ClearSlot()
    {
        image.SetActive(false);
    }

    public void TogglePopup()
    {
        CharSheetPopup.Instance.ShowPopup(charSheet);
    }
}

