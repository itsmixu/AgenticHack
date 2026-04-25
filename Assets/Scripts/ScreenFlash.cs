using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    [SerializeField] private Image flashImage;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    // Public method to start fade in
    public void FadeIn(Color color, float duration = 0.5f, float maxAlpha = 1f)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(color, 0f, maxAlpha, duration));
    }

    // Public method to start fade out
    public void FadeOut(Color color, float duration = 0.5f, float maxAlpha = 1f)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(color, maxAlpha, 0f, duration));
    }

    // General coroutine used by both FadeIn and FadeOut
    private IEnumerator FadeRoutine(Color color, float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        Color fadeColor = color;
        fadeColor.a = startAlpha;
        flashImage.color = fadeColor;

        while (timer < duration)
        {
            float alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            fadeColor.a = alpha;
            flashImage.color = fadeColor;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        fadeColor.a = endAlpha;
        flashImage.color = fadeColor;
    }
}
