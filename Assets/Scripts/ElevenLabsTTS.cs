using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ElevenLabsTTS : MonoBehaviour
{
    public static ElevenLabsTTS Instance;

    [Header("ElevenLabs")]
    [SerializeField] private string apiKey;
    [SerializeField] private string modelId = "eleven_multilingual_v2";
    [SerializeField] private AudioSource audioSource;

    private const string BaseUrl = "https://api.elevenlabs.io/v1/text-to-speech/";

    [Serializable]
    private class TtsPayload
    {
        public string text;
        public string model_id;
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(apiKey) && audioSource != null;
    }

    public IEnumerator Speak(string text, string voiceId)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(voiceId) || !IsConfigured())
            yield break;

        string requestUrl = BaseUrl + voiceId;
        TtsPayload payload = new TtsPayload
        {
            text = text,
            model_id = modelId
        };

        string payloadJson = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(payloadJson);

        using (UnityWebRequest request = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "audio/mpeg");
            request.SetRequestHeader("xi-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("ElevenLabs TTS request failed: " + request.error);
                yield break;
            }

            string tempPath = Path.Combine(Application.temporaryCachePath, "elevenlabs_npc_tts.mp3");
            File.WriteAllBytes(tempPath, request.downloadHandler.data);

            using (UnityWebRequest clipRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
            {
                yield return clipRequest.SendWebRequest();

                if (clipRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("ElevenLabs audio load failed: " + clipRequest.error);
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(clipRequest);
                if (clip == null)
                    yield break;

                audioSource.Stop();
                audioSource.clip = clip;
                audioSource.Play();

                while (audioSource.isPlaying)
                    yield return null;
            }
        }
    }
}
