using System;
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public float fadeDuration = 1f;
    public Sound[] sounds;

    private Coroutine musicFadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(string soundName)
    {
        Sound s = Array.Find(sounds, sound => sound.name == soundName);
        if (s.clip != null)
            sfxSource.PlayOneShot(s.clip);
        else
            Debug.LogWarning("Sound not found: " + soundName);
    }

    public void PlayMusic(string soundName)
    {
        Sound s = Array.Find(sounds, sound => sound.name == soundName);
        if (s.clip != null)
        {
            if (musicSource.clip == s.clip && musicSource.isPlaying)
                return;
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeMusic(s.clip));
        }
        else
        {
            Debug.LogWarning("Sound not found: " + soundName);
        }
    }

    public void StopMusic()
    {
        if (musicFadeCoroutine != null)
            StopCoroutine(musicFadeCoroutine);
        if (musicSource.isPlaying)
            musicSource.Stop();
    }

    private IEnumerator FadeMusic(AudioClip newClip)
    {
        float startVolume = musicSource.volume;
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = 0;
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0, startVolume, t / fadeDuration);
            yield return null;
        }
        musicSource.volume = startVolume;
    }
}
