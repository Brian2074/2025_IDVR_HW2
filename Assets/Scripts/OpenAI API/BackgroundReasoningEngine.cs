using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class BackgroundReasoningEngine : MonoBehaviour
{
    // This is a web request example of how to interact with the OpenAI GPT API in Unity,
    // [Warning!!] GPT-5 model is not available for the web request url, please use GPT-4.1 or other available models.

    public string apiKey = "YOUR_API_KEY";
    public string model = "gpt-4o-mini"; 
    
    [Header("References")]
    public AnimationHandler animationHandler; // 新增：用來控制 Avatar

    [HideInInspector] public List<Dictionary<string, string>> chatHistory = new List<Dictionary<string, string>>();
    [HideInInspector] public Queue<string> responseBuffer = new Queue<string>();

    // 儲存當前的對話與情境
    private string lastAIResponse = "";
    private string currentObjectContext = "None";
    
    void Start()
    {
        // 訂閱 RealtimeAPI 的事件 (當 AI 說完話時)
        RealtimeAPIWrapper.OnAIResponseFinished += HandleAIResponse;
        
        // 訂閱 ContextHandler 的事件 (當情境改變時)
        ContextAwareObjectHandler.OnContextChanged += HandleContextChange;
    }

    void OnDestroy()
    {
        RealtimeAPIWrapper.OnAIResponseFinished -= HandleAIResponse;
        ContextAwareObjectHandler.OnContextChanged -= HandleContextChange;
    }

    // 當 AI 回應結束後，觸發背景推理
    private void HandleAIResponse(string transcript)
    {
        lastAIResponse = transcript;
        AnalyzeSituation();
    }

    // 當情境改變時更新狀態
    private void HandleContextChange(string newContext)
    {
        currentObjectContext = newContext;
        Debug.Log($"[StructuredOutput] Context Updated: {currentObjectContext}");
    }

    // 組合 Prompt 並發送請求
    private void AnalyzeSituation()
    {
        string prompt = $"The user is in a VR environment. \n" +
                        $"Current Context/Interaction: {currentObjectContext}\n" +
                        $"The AI Assistant just said: \"{lastAIResponse}\"\n\n" +
                        $"Based on this, determine if the avatar should perform an animation to match the context or speech.\n" +
                        $"Available animations: 'wave', 'dance', 'clap', 'idle'.";

        // 呼叫原本的結構化請求邏輯
        StartCoroutine(SendStructuredChatRequest(prompt));
    }

    void Update()
    {
        // Check if there are any responses in the buffer and process them.
        if (responseBuffer.Count > 0)
        {
            string response = responseBuffer.Dequeue();
            Debug.Log("GPT Response: " + response);
        }
    }

    public void SendPrompt(string prompt)
    {
        StartCoroutine(SendChatRequest(prompt));
    }

    public void SendStructuredPrompt(string prompt)
    {
        StartCoroutine(SendStructuredChatRequest(prompt));
    }

    IEnumerator SendChatRequest(string prompt)
    {
        // Endpoint URL
        var url = "https://api.openai.com/v1/chat/completions";

        // Notice: Here would refresh the chat history every time a new prompt is sent.
        var messages = new List<Dictionary<string, string>> {
            new Dictionary<string, string> {
                { "role", "user" },
                { "content", prompt }
            }
        };

        // Request body
        var requestBody = new
        {
            model = model,
            messages = messages,
            temperature = 0.7
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody); // Serialize the request body to JSON
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("RAW Response: " + request.downloadHandler.text);
                var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);

                // Once the message is received, add it to the buffer.
                if (response.choices != null && response.choices.Count > 0)
                {
                    responseBuffer.Enqueue(response.choices[0].message.content);
                }
                
                // Notice: If you need complete chat rather then a single request, you should maintain the chat history.
            }
            else
            {
                Debug.LogError("GPT Request Error: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text);
            }
        }
    }

    IEnumerator SendStructuredChatRequest(string prompt)
    {
        var url = "https://api.openai.com/v1/chat/completions";

        // Messages
        var messages = new List<Dictionary<string, string>> {
            new Dictionary<string, string> {
                { "role", "user" },
                { "content", prompt }
            }
        };

        var responseFormat = new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "avatar_control", // 修改 Schema 名稱以符合用途
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        animation = new 
                        { 
                            type = "string", 
                            description = "The animation to trigger.",
                            @enum = new[] { "wave", "dance", "clap", "idle" }
                        },
                        reasoning = new { type = "string" }
                    },
                    required = new[] { "animation", "reasoning" },
                    additionalProperties = false
                },
                strict = true
            }
        };

        // Request body
        var requestBody = new
        {
            model = model,
            messages = messages,
            temperature = 0.5, // 稍微調高一點讓它有創意，但不要太高
            response_format = responseFormat
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("RAW Response: " + request.downloadHandler.text);

                var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);

                if (response.choices != null && response.choices.Count > 0)
                {
                    string json = response.choices[0].message.content;
                    Debug.Log("Structured Output: " + json);

                    // Optional: parse into a C# model
                    try 
                    {
                        var result = JsonConvert.DeserializeObject<AvatarControlResult>(json);
                        Debug.Log($"[Analysis] Animation: {result.animation}, Reasoning: {result.reasoning}");
                        
                        if (result.animation != "idle" && animationHandler != null)
                        {
                            animationHandler.PlayAnimation(result.animation);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error parsing structured output: " + e.Message);
                    }
                }
            }
            else
            {
                Debug.LogError("GPT Request Error: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text);
            }
        }
    }

    [System.Serializable]
    public class OpenAIResponse
    {
        public List<Choice> choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    // Structured Output Format C# Model
    [System.Serializable]
    public class AvatarControlResult
    {
        public string animation;
        public string reasoning;
    }
}
