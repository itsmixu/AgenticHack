using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class EndScreen : MonoBehaviour
{
    public static EndScreen Instance;

    [SerializeField] private TextMeshProUGUI endText;
    [SerializeField] private GameObject bg;
    [SerializeField] private GameObject ghost;
    [SerializeField] private GameObject button;

    private void Awake()
    {
        Instance = this;
    }
        
    public void ShowEndScreen(string EndText, bool ShowGhost)
    {
        bg.SetActive(true);
        endText.enabled = true;
        endText.text = EndText;
        button.SetActive(true);

        if (ShowGhost) ghost.SetActive(true);
    }


    public void Restart()
    {
        SceneManager.LoadScene(1);
    }
}
