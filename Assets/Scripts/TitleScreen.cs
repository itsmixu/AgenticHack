using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    public void StartGame()
    {
        StartCoroutine(StartTheGame());
    }

    private IEnumerator StartTheGame()
    {
        ScreenFlash.Instance.FadeIn(Color.black, 2f);
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(1);
    }
}
