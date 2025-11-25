using UnityEngine;
using System;
using System.Text;
using System.Text.Json;
using System.Collections;
using System.Collections.Generic;
using OpenAI.Responses;
using OpenAI.Chat;
using OpenAI.Conversations;

public class GPTRequest : MonoBehaviour
{
    public string apiKey = "YOUR_API_KEY";
    public string model = "gpt-5-nano";

    // Chat API
    private ChatClient chatClient;
    List<ChatMessage> ChatHistory = new List<ChatMessage>
    {
        // TODO: Manage system prompts based on your needs.
        new SystemChatMessage("You are a helpful assistant."),
    };

    #region Chat API Example
    void Start()
    {
        // [Example of AI chat conversation] (Annotation avoiding spending tokens)

        // CreateConversationClient();
        // GetReplyFromChat("Hello! Briefly introduce the difference between VR, and MR.");
    }

    void CreateConversationClient()
    {
        // Create the chat client
        chatClient = new ChatClient(
            model: model,
            apiKey: apiKey
        );
    }

    void GetReplyFromChat(string newMsg)
    {
        // TODO: Manage conversation based on your needs.
        // Add the new message to the chat history and get a completion
        ChatHistory.Add(new UserChatMessage(newMsg));
        ChatCompletion completion = chatClient.CompleteChat(ChatHistory);

        Debug.Log("[GPT Chat NewReply]: " + completion.Content[0].Text);
    }

    void GetStructuredReplyFromChat(string newMsg)
    {
        // TODO: Implement your own structured response handling here.
        // Below is just an example for math reasoning steps with structured JSON response.

        List<ChatMessage> messages = new List<ChatMessage>
        {
            new UserChatMessage("How can I solve 8x + 7 = -23?"),
        };

        ChatCompletionOptions options = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "math_reasoning",
                jsonSchema: BinaryData.FromBytes(Encoding.UTF8.GetBytes(@"
                    {
                        ""type"": ""json_schema"",
                        ""properties"": {
                            ""steps"": {
                                ""type"": ""array"",
                                ""items"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""explanation"": { ""type"": ""string"" },
                                        ""output"": { ""type"": ""string"" }
                                    },
                                    ""required"": [""explanation"", ""output""],
                                    ""additionalProperties"": false
                                }
                            },
                            ""final_answer"": { ""type"": ""string"" }
                        },
                        ""required"": [""steps"", ""final_answer""],
                        ""additionalProperties"": false
                    }
                    ")),
                jsonSchemaIsStrict: true)
        };

        ChatCompletion completion = chatClient.CompleteChat(messages, options);

        using JsonDocument structuredJson = JsonDocument.Parse(completion.Content[0].Text);

        Debug.Log($"Final answer: {structuredJson.RootElement.GetProperty("final_answer")}");
        Debug.Log("Reasoning steps:");

        foreach (JsonElement stepElement in structuredJson.RootElement.GetProperty("steps").EnumerateArray())
        {
            Debug.Log($"  - Explanation: {stepElement.GetProperty("explanation")}");
            Debug.Log($"    Output: {stepElement.GetProperty("output")}");
        }
    }
    #endregion
}