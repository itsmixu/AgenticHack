using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    private void Start()
    {
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.canMove = false;
        else
            Debug.LogWarning("TutorialPopup: PlayerMovement.Instance is null in Start().");
    }
    public void ClosePopup()
    {
        gameObject.SetActive(false);
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.canMove = true;
        else
            Debug.LogWarning("TutorialPopup: PlayerMovement.Instance is null in ClosePopup().");
    }
}
