using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharSheetPopup : MonoBehaviour
{
    public static CharSheetPopup Instance;

    [SerializeField] private GameObject popup;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Image image;

    private CharacterSheet lastSheet;

    private void Awake()
    {
        Instance = this;
        popup.SetActive(false);
    }

    public void ShowPopup(CharacterSheet item)
    {
        if (lastSheet != null)
        {
            lastSheet.note = inputField.text;
        }

        inputField.text = item.note;
        image.sprite = item.name;
        popup.SetActive(true);
        lastSheet = item;
        PlayerMovement.Instance.canMove = false;
    }


    public void ClosePopup()
    {
        popup.SetActive(false);
        PlayerMovement.Instance.canMove = true;
    }
}
