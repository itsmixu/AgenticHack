using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    private void Start()
    {
        PlayerMovement.Instance.canMove = false;
    }
    public void ClosePopup()
    {
        gameObject.SetActive(false);
        PlayerMovement.Instance.canMove = true;
    }
}
