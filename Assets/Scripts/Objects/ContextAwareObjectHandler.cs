using UnityEngine;

public class ContextAwareObjectHandler : MonoBehaviour
{
    // Here is just a basic trick of how context-aware objects can be implemented.
    // You can expand upon this by adding more complex interactions and context handling.

    [SerializeField] private string objectName;
    [TextArea]
    [SerializeField] private string objectDescription;

    private RealtimeAPIWrapper realtimeAPIWrapper;

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
            SendContextToAPI(contextPrompt);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name.Contains("Hand") || other.name.Contains("PinchArea"))
        {
            string contextPrompt = $"The user has stopped interacting with the object: {objectName}.";
            SendContextToAPI(contextPrompt);
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
