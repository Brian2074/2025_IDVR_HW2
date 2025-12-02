using UnityEngine;

public class ContextAwareObjectHandler : MonoBehaviour
{
    // Here is just a basic trick of how context-aware objects can be implemented.
    // You can expand upon this by adding more complex interactions and context handling.

    [SerializeField] private string objectName;
    [TextArea]
    [SerializeField] private string objectDescription;

    private RealtimeAPIWrapper realtimeAPIWrapper;
    
    // 新增：事件通知
    public static event System.Action<string> OnContextChanged;

    void Start()
    {
        realtimeAPIWrapper = GameObject.FindObjectOfType<RealtimeAPIWrapper>();
    }

    # region Example of Interaction Handling
    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Hand") || other.name.Contains("PinchArea"))
        {
            string contextPrompt = $"The user is interacting with the object: {objectName}. Description: {objectDescription}.";
            
            // 1. 傳送給 Realtime API (讓對話知道)
            SendContextToAPI(contextPrompt);
            
            // 2. 觸發事件 (讓 Background LLM 知道)
            OnContextChanged?.Invoke(contextPrompt);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name.Contains("Hand") || other.name.Contains("PinchArea"))
        {
            string contextPrompt = $"The user has stopped interacting with the object: {objectName}.";
            
            // 1. 傳送給 Realtime API
            SendContextToAPI(contextPrompt);
            
            // 2. 觸發事件
            OnContextChanged?.Invoke("None");
        }
    }
    # endregion
    
    // Send context information to the Realtime API
    void SendContextToAPI(string contextPrompt)
    {
        if (realtimeAPIWrapper != null && RealtimeAPIConnection.instance.isConnected)
        {
            realtimeAPIWrapper.SendTextToAPI(contextPrompt);
        }
    }
}
