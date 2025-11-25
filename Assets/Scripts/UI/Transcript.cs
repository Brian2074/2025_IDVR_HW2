using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Transcript : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject MessageBlockPrefab;
    [SerializeField] private Transform panelTransform;

    string currentConversationLine = "";
    GameObject newMsgBlock = null;

    int logCountLimit = 14;
    List<string> logMessages = new List<string>();
    List<string> conversationMessages = new List<string>();

    [SerializeField] private AudioRecorder audioRecorder;
    [SerializeField] private GameObject recordingStateText;
    [SerializeField] private Image[] frequencyBars;
    [SerializeField] private TextMeshProUGUI connectionStatusText;

    float maxFrequencyAmplitude = 4f;
    float barSmoothingSpeed = 5f;
    float[] userBarAmplitudes;
    bool isRecording = false;


    // string currentConversationLine = "";
    // Start is called before the first frame update
    void Start()
    {
        RealtimeAPIWrapper.OnSessionCreated += OnSessionCreated;
        RealtimeAPIWrapper.OnConversationItemCreated += OnConversationItemCreated;
        RealtimeAPIWrapper.OnResponseDone += OnResponseDone;
        RealtimeAPIWrapper.OnTranscriptReceived += OnTranscriptReceived;
        RealtimeAPIWrapper.OnResponseCreated += OnResponseCreated;
        RealtimeAPIWrapper.OnConnectButtonPressed += OnConnectButtonPressed;

        AudioRecorder.OnVADRecordingStarted += OnVADRecordingStarted;
        AudioRecorder.OnVADRecordingEnded += OnVADRecordingEnded;

        userBarAmplitudes = new float[frequencyBars.Length];
    }

    void Update()
    {
        UpdateFrequencyBars();
    }

    private void UpdateFrequencyBars()
    {
        if (frequencyBars == null || frequencyBars.Length == 0)
            return;

        if (!isRecording && audioRecorder.listeningMode == ListeningMode.PushToTalk)
        {
            for (int i = 0; i < frequencyBars.Length; i++)
            {
                userBarAmplitudes[i] = Mathf.Lerp(userBarAmplitudes[i], 0f, Time.deltaTime * barSmoothingSpeed);
                frequencyBars[i].fillAmount = userBarAmplitudes[i];
            }
            return;
        }

        float[] spectrum = audioRecorder.frequencyData;
        if (spectrum == null || spectrum.Length == 0)
        {
            for (int i = 0; i < frequencyBars.Length; i++)
            {
                userBarAmplitudes[i] = Mathf.Lerp(userBarAmplitudes[i], 0f, Time.deltaTime * barSmoothingSpeed);
                frequencyBars[i].fillAmount = userBarAmplitudes[i];
            }
            return;
        }

        float sampleRate = audioRecorder.sampleRate;
        int fftSize = audioRecorder.fftSampleSize;
        float nyquist = sampleRate / 2f;
        float freqPerBin = nyquist / fftSize;
        float[] freqBands = new float[] { 85f, 160f, 255f, 350f, 500f, 1000f, 2000f, 3000f, 4000f, nyquist };

        for (int i = 0; i < frequencyBars.Length; i++)
        {
            int startIndex = i == 0 ? 0 : Mathf.FloorToInt(freqBands[i - 1] / freqPerBin);
            int endIndex = Mathf.FloorToInt(freqBands[i] / freqPerBin);
            float sum = 0f;
            for (int j = startIndex; j < endIndex; j++)
            {
                sum += spectrum[j];
            }
            int sampleCount = endIndex - startIndex;
            float average = sampleCount > 0 ? sum / sampleCount : 0f;
            float amplitude = average * Mathf.Pow(2f, i);
            amplitude = Mathf.Clamp01(amplitude / maxFrequencyAmplitude);
            userBarAmplitudes[i] = Mathf.Lerp(userBarAmplitudes[i], amplitude, Time.deltaTime * barSmoothingSpeed);
            frequencyBars[i].fillAmount = userBarAmplitudes[i];
        }
    }

    private void OnConversationItemCreated()
    {

        if (!string.IsNullOrEmpty(currentConversationLine))
        {
            if (conversationMessages.Count >= logCountLimit) conversationMessages.RemoveAt(0);
            conversationMessages.Add(currentConversationLine);
        }

        currentConversationLine = "";
        newMsgBlock = null;
    }

    private void OnTranscriptReceived(string transcriptPart)
    {
        if (string.IsNullOrEmpty(currentConversationLine))
            newMsgBlock = Instantiate(MessageBlockPrefab, panelTransform);

        currentConversationLine += transcriptPart;
        newMsgBlock.GetComponentInChildren<TextMeshProUGUI>().text = currentConversationLine;

        StartCoroutine(ScrollToBottomLater());
    }

    private void OnVADRecordingStarted()
    {
        recordingStateText.SetActive(true);
    }

    private void OnVADRecordingEnded()
    {
        recordingStateText.SetActive(false);
    }

    private void OnConnectButtonPressed()
    {
        connectionStatusText.text = RealtimeAPIConnection.instance.connectionStatus;
    }

    private void OnSessionCreated() => Debug.Log("session created.");
    private void OnResponseCreated() => Debug.Log("response created.");
    private void OnResponseDone() => Debug.Log("response done.");

    // For UI
    IEnumerator ScrollToBottomLater()
    {
        yield return new WaitForSeconds(0.1f);
        var content = scrollRect.content;
        var viewport = scrollRect.viewport;

        float newY = Mathf.Max(0, content.sizeDelta.y - viewport.rect.height);
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, newY);
    }
}
