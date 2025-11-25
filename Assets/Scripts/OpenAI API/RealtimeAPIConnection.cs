using UnityEditor;
using UnityEngine;

public class RealtimeAPIConnection : MonoBehaviour
{
    public static RealtimeAPIConnection instance;
    [HideInInspector] public bool isConnected = false;

    public string connectionStatus => isConnected ? "Connected" : "Disconnected";
    public string connectionButtonString => isConnected ? "Disconnect" : "Connect";

    private RealtimeAPIWrapper realtimeWrapper;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        realtimeWrapper = GetComponent<RealtimeAPIWrapper>();
    }

    void Update()
    {
        // 1. Connect via controller (X button)
        if (OVRInput.GetDown(OVRInput.RawButton.Y))
        {
            isConnected = !isConnected;
            realtimeWrapper.ConnectWebSocketButton();
        }
    }

    // 2. Connect via UI (Scene GUI)
    public void sceneGUIButtonPressed()
    {
        // in realtime API wrapper, this will toggle the connection status
        realtimeWrapper.ConnectWebSocketButton();
    }
}
