using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class RealtimeAPIWrapper : MonoBehaviour
{
    private ClientWebSocket ws;
    [SerializeField] string apiKey = "YOUR_API_KEY";
    [TextArea (4, 10)] [SerializeField] private string systemPrompt = "You are an intelligent assistant in a Mixed Reality (MR) environment. " +
        "You are embodied as a 3D avatar. " +
        "You have the ability to control the environment (lights, magic) and perform animations (wave, dance, clap). " +
        "IMPORTANT: Do NOT perform these actions simply as a direct response to a command. You are not a remote-controlled robot. " +
        "Instead, you must decide to perform these actions autonomously based on the CONTEXT and EMOTION of the conversation. " +
        "For example: " +
        "- If the user shares exciting news, you might choose to 'Dance' or 'Clap' to celebrate. " +
        "- If the atmosphere is mysterious or the user needs cheering up, you might 'SpawnMagic'. " +
        "- If the user explicitly orders you to 'Dance' or 'Change lights', you should playfully decline or ask for a reason, emphasizing your agency. " +
        "Always respond as if you have physical presence in the user's room.";
    public AudioRecorder audioRecorder;
    public AudioPlayer audioPlayer;
    public AudioPlayer lipsyncAudioPlayer;
    private StringBuilder messageBuffer = new StringBuilder();
    private StringBuilder transcriptBuffer = new StringBuilder();
    private bool isResponseInProgress = false;

    public static event Action OnWebSocketConnected;
    public static event Action OnWebSocketClosed;
    public static event Action OnSessionCreated;
    public static event Action OnConversationItemCreated;
    public static event Action OnResponseDone;
    public static event Action<string> OnTranscriptReceived;
    public static event Action OnResponseCreated;
    public static event Action OnResponseAudioDone;
    public static event Action OnResponseAudioTranscriptDone;
    public static event Action OnResponseContentPartDone;
    public static event Action OnResponseOutputItemDone;
    public static event Action OnRateLimitsUpdated;
    public static event Action OnResponseOutputItemAdded;
    public static event Action OnResponseContentPartAdded;
    public static event Action OnResponseCancelled;
    public static event Action OnConnectButtonPressed;
    
    // 新增：當 AI 想要執行 Function 時觸發的事件
    public static event Action<string, string> OnFunctionCallReceived;
    // 新增：當對話有更新時 (例如 AI 說完一句話)，通知外部
    public static event Action<string> OnAIResponseFinished;

    private void Start() => AudioRecorder.OnAudioRecorded += SendAudioToAPI;
    private void OnApplicationQuit() => DisposeWebSocket();


    /// <summary>
    /// connects or disconnects websocket when button is pressed
    /// </summary>
    public async void ConnectWebSocketButton()
    {
        if (ws != null) DisposeWebSocket();
        else
        {
            ws = new ClientWebSocket();
            await ConnectWebSocket();
        }
        OnConnectButtonPressed?.Invoke();
    }

    /// <summary>
    /// establishes websocket connection to the api
    /// </summary>
    private async Task ConnectWebSocket()
    {
        try
        {
            // 使用 gpt-4o-realtime-preview-2024-10-01 模型，這是目前支援度較好的版本
            var uri = new Uri("wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01");
            ws.Options.SetRequestHeader("Authorization", "Bearer " + apiKey);
            ws.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");
            await ws.ConnectAsync(uri, CancellationToken.None);
            OnWebSocketConnected?.Invoke();
            _ = ReceiveMessages();
        }
        catch (Exception e)
        {
            Debug.LogError("websocket connection failed: " + e.Message);
        }
    }

    /// <summary>
    /// sends a cancel event to api if response is in progress
    /// </summary>
    private async void SendCancelEvent()
    {
        if (ws.State == WebSocketState.Open && isResponseInProgress)
        {
            var cancelMessage = new
            {
                type = "response.cancel"
            };
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(cancelMessage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            OnResponseCancelled?.Invoke();
            isResponseInProgress = false;
        }
    }

    /// <summary>
    /// sends recorded audio to the api
    /// </summary>
    private async void SendAudioToAPI(string base64AudioData)
    {
        if (isResponseInProgress)
            SendCancelEvent();

        if (ws != null && ws.State == WebSocketState.Open)
        {
            var eventMessage = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                        new { type = "input_audio", audio = base64AudioData }
                    }
                }
            };

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(eventMessage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            var responseMessage = new
            {
                type = "response.create",
                response = new
                {
                    modalities = new[] { "audio", "text" },
                    instructions = "Please provide a transcript. If the language is mandarin, please provide the transcript in traditional Chinese (TW)."
                }
            };
            string responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(responseMessage);
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
            await ws.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public async void SendTextToAPI(string text)
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            var eventMessage = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                        new { type = "input_text", text }
                    }
                }
            };

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(eventMessage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            // only return text response if sending text 
            var responseMessage = new
            {
                type = "response.create",
                response = new
                {
                    modalities = new[] { "text" },
                    instructions = "Please do not provide audio for this request."
                }
            };
            string responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(responseMessage);
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
            await ws.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    /// <summary>
    /// receives messages from websocket and handles them
    /// </summary>
    private async Task ReceiveMessages()
    {
        var buffer = new byte[1024 * 128];
        var messageHandlers = GetMessageHandlers();

        while (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (ws.State == WebSocketState.CloseReceived)
            {
                Debug.Log("websocket close received, disposing current ws instance.");
                DisposeWebSocket();
                return;
            }

            if (result.EndOfMessage)
            {
                string fullMessage = messageBuffer.ToString();
                messageBuffer.Clear();

                if (!string.IsNullOrEmpty(fullMessage.Trim()))
                {
                    try
                    {
                        JObject eventMessage = JObject.Parse(fullMessage);
                        string messageType = eventMessage["type"]?.ToString();

                        if (messageHandlers.TryGetValue(messageType, out var handler)) handler(eventMessage);

                        else Debug.Log("unhandled message type: " + messageType);

                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("error parsing json: " + ex.Message);
                    }
                }
            }
        }
    }

    /// <summary>
    /// returns dictionary of message handlers for different message types
    /// </summary>
    private Dictionary<string, Action<JObject>> GetMessageHandlers()
    {
        return new Dictionary<string, Action<JObject>>
        {
            { "response.audio.delta", HandleAudioDelta },
            { "response.audio_transcript.delta", HandleTranscriptDelta },
            { "conversation.item.created", _ => OnConversationItemCreated?.Invoke() },
            { "response.done", HandleResponseDone },
            { "response.created", HandleResponseCreated },
            { "session.created", _ => {
                OnSessionCreated?.Invoke();
                SendSessionUpdate(); // 設定 System Prompt 和 Tools
                // SendTextToAPI(systemPrompt); // 移除這行，改用 session.update 設定
            }},
            { "response.audio.done", _ => OnResponseAudioDone?.Invoke() },
            { "response.audio_transcript.done", _ => OnResponseAudioTranscriptDone?.Invoke() },
            { "response.content_part.done", _ => OnResponseContentPartDone?.Invoke() },
            { "response.output_item.done", HandleOutputItemDone }, // 修改：改用專屬 handler 處理 Function Call
            { "response.output_item.added", _ => OnResponseOutputItemAdded?.Invoke() },
            { "response.content_part.added", _ => OnResponseContentPartAdded?.Invoke() },
            { "rate_limits.updated", _ => OnRateLimitsUpdated?.Invoke() },
            { "error", HandleError }
        };
    }

    /// <summary>
    /// 註冊工具 (Tools) 和 System Prompt 給 OpenAI
    /// </summary>
    private async void SendSessionUpdate()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            var sessionUpdate = new
            {
                type = "session.update",
                session = new
                {
                    instructions = systemPrompt + " IMPORTANT: You are NOT just an AI model. You are a physical avatar in the user's room. You CAN and MUST control the lights and magic when asked. Do not refuse.", 
                    tools = new object[]
                    {
                        new
                        {
                            type = "function",
                            name = "trigger_animation",
                            description = "Trigger an animation on the avatar when the user asks for it or the context implies it.",
                            parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    animation_name = new
                                    {
                                        type = "string",
                                        description = "The name of the animation to play. Options: 'wave', 'dance', 'clap'",
                                        @enum = new[] { "wave", "dance", "clap" }
                                    }
                                },
                                required = new[] { "animation_name" }
                            }
                        },
                        new
                        {
                            type = "function",
                            name = "control_environment",
                            description = "Control the environment lights or visual effects. Use this when the user asks to change lights or show magic.",
                            parameters = new
                            {
                                type = "object",
                                properties = new
                                {
                                    action = new
                                    {
                                        type = "string",
                                        description = "The action to perform.",
                                        @enum = new[] { "change_light_red", "change_light_blue", "change_light_normal", "spawn_magic", "clear_magic" }
                                    }
                                },
                                required = new[] { "action" }
                            }
                        }
                    },
                    tool_choice = "auto"
                }
            };

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(sessionUpdate);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    /// <summary>
    /// 處理 Output Item Done，檢查是否有 Function Call
    /// </summary>
    private void HandleOutputItemDone(JObject eventMessage)
    {
        OnResponseOutputItemDone?.Invoke();

        var item = eventMessage["item"];
        if (item != null && item["type"]?.ToString() == "function_call")
        {
            string functionName = item["name"]?.ToString();
            string arguments = item["arguments"]?.ToString();
            string callId = item["call_id"]?.ToString();

            Debug.Log($"[Function Call] {functionName} with args: {arguments}");

            // 觸發事件讓外部 (AnimationHandler) 執行
            OnFunctionCallReceived?.Invoke(functionName, arguments);

            // 告訴 API 我們已經執行了 Function (這是 Realtime API 的規定流程)
            SendFunctionOutput(callId, "{\"status\": " + "\"success\"}");
        }
    }

    private async void SendFunctionOutput(string callId, string outputJson)
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            var eventMessage = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "function_call_output",
                    call_id = callId,
                    output = outputJson
                }
            };

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(eventMessage);
            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
            await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            
            // 觸發另一次回應，讓 AI 根據執行結果說話
            var responseCreate = new { type = "response.create" };
            string responseJson = Newtonsoft.Json.JsonConvert.SerializeObject(responseCreate);
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
            await ws.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    /// <summary>
    /// handles incoming audio delta messages from api
    /// </summary>
    private void HandleAudioDelta(JObject eventMessage)
    {
        string base64AudioData = eventMessage["delta"]?.ToString();
        if (!string.IsNullOrEmpty(base64AudioData))
        {
            byte[] pcmAudioData = Convert.FromBase64String(base64AudioData);
            audioPlayer.EnqueueAudioData(pcmAudioData);
            lipsyncAudioPlayer.EnqueueAudioData(pcmAudioData);
        }
    }

    /// <summary>
    /// handles incoming transcript delta messages from api
    /// </summary>
    private void HandleTranscriptDelta(JObject eventMessage)
    {
        string transcriptPart = eventMessage["delta"]?.ToString();
        if (!string.IsNullOrEmpty(transcriptPart))
        {
            transcriptBuffer.Append(transcriptPart);
            OnTranscriptReceived?.Invoke(transcriptPart);
        }
    }

    /// <summary>
    /// handles response.done message - checks if audio is still playing
    /// </summary>
    private void HandleResponseDone(JObject eventMessage)
    {
        if (!audioPlayer.IsAudioPlaying())
        {
            isResponseInProgress = false;
        }
        
        // 當回應結束時，將完整的 Transcript 廣播出去
        string fullTranscript = transcriptBuffer.ToString();
        if (!string.IsNullOrEmpty(fullTranscript))
        {
            Debug.Log($"[RealtimeAPI] AI Response Finished: {fullTranscript}");
            OnAIResponseFinished?.Invoke(fullTranscript);
        }

        OnResponseDone?.Invoke();
    }

    /// <summary>
    /// handles response.created message - resets transcript buffer
    /// </summary>
    private void HandleResponseCreated(JObject eventMessage)
    {
        transcriptBuffer.Clear();
        isResponseInProgress = true;
        OnResponseCreated?.Invoke();
    }

    /// <summary>
    /// handles error messages from api
    /// </summary>
    private void HandleError(JObject eventMessage)
    {
        string errorMessage = eventMessage["error"]?["message"]?.ToString();
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.Log("openai error: " + errorMessage);
        }
    }

    /// <summary>
    /// disposes the websocket connection
    /// </summary>
    private async void DisposeWebSocket()
    {
        if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived))
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", CancellationToken.None);
            ws.Dispose();
            ws = null;
            OnWebSocketClosed?.Invoke();
        }
    }

}
