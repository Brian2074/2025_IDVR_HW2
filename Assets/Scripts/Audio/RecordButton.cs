using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordButton : MonoBehaviour
{
    [SerializeField] private AudioRecorder audioRecorder;
    [SerializeField] private GameObject buttonObject;
    // [SerializeField] private Button pushToTalkButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Image buttonTextBackground;
    bool isRecording = false;
    
    // Start is called before the first frame update
    void Start()
    {
        // pushToTalkButton.onClick.AddListener(OnRecordButtonPressed);
        UpdateRecordButton();
    }

    private void OnRecordButtonPressed()
    {
        if (audioRecorder.listeningMode == ListeningMode.PushToTalk)
        {
            if (isRecording) StopRecording();
            else StartRecording();
        }
    }

    public void StartRecording()
    {
        audioRecorder.StartRecording();
        isRecording = true;
        Debug.Log("recording...");
        UpdateRecordButton();
    }

    public void StopRecording()
    {
        audioRecorder.StopRecording();
        isRecording = false;
        Debug.Log("recording stopped. sending audio...");
        UpdateRecordButton();
    }

    private void UpdateRecordButton()
    {
        if (audioRecorder.listeningMode == ListeningMode.PushToTalk)
        {
            buttonObject.SetActive(true);
            
            // pushToTalkButton.interactable = true;
            if (isRecording)
            {
                buttonText.text = "Release to send";

            }
            else
            {
                buttonText.text = "Push to talk";
            }
        }
        else
        {
            buttonObject.SetActive(false);
        }
    }
}
