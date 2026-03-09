using ChatApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ChatApp.Infrastructure.Services;

public class AiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<AiService> _logger;
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiService(IConfiguration config, ILogger<AiService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _apiKey = config["Groq:ApiKey"]!;
        _httpClient = httpClientFactory.CreateClient("Groq");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }
     
    private async Task<string> CallGroqAsync(string prompt)
    {
        var requestBody = new
        {
            model = "llama-3.3-70b-versatile", 
            messages = new[]
            {
            new { role = "user", content = prompt }
        },
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(
            "Authorization", $"Bearer {_apiKey}");

        _logger.LogInformation("Calling Groq API...");
        var response = await _httpClient.PostAsync(BaseUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Groq API error: {Status} {Body}",
                response.StatusCode, responseBody);
            throw new Exception($"Groq API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        _logger.LogInformation("Groq response received successfully");
        return text?.Trim() ?? string.Empty;
    }
     
    public async Task<List<string>> GetSmartRepliesAsync(
        string lastMessage, string conversationContext)
    {
        try
        {
            _logger.LogInformation("Generating smart replies");

            var prompt = $"""
                You are a smart reply assistant for a chat app.
                
                Conversation context:
                {conversationContext}
                
                Last message received: "{lastMessage}"
                
                Generate exactly 3 short, natural reply suggestions.
                Each reply should be different in tone (friendly, neutral, casual).
                Keep each reply under 10 words.
                
                Respond ONLY with a JSON array of 3 strings. No explanation. No markdown.
                Example: ["Sure, sounds good!", "Let me check and get back", "Yeah why not!"]
                """;

            var raw = await CallGroqAsync(prompt);
            raw = raw.Replace("```json", "").Replace("```", "").Trim();

            var suggestions = JsonSerializer.Deserialize<List<string>>(raw, _jsonOptions)
                ?? new List<string>();

            _logger.LogInformation("Generated {Count} smart replies", suggestions.Count);
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart replies");
            return new List<string> { "Ok!", "Sure!", "Got it!" };
        }
    }
     
    public async Task<string> TranslateMessageAsync(
        string message, string targetLanguage)
    {
        try
        {
            _logger.LogInformation("Translating to {Language}", targetLanguage);

            var prompt = $"""
                Translate the following message to {targetLanguage}.
                Return ONLY the translated text, nothing else.
                Do not add any explanation or quotes.
                
                Message: {message}
                """;

            var translated = await CallGroqAsync(prompt);
            _logger.LogInformation("Translation completed");
            return translated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating message");
            return message;
        }
    }
     
    public async Task<string> SummarizeConversationAsync(List<string> messages)
    {
        try
        {
            _logger.LogInformation("Summarizing {Count} messages", messages.Count);

            var conversation = string.Join("\n", messages);

            var prompt = $"""
                Summarize the following chat conversation in 2-3 sentences.
                Focus on key points, decisions made, and action items.
                Be concise and clear.
                
                Conversation:
                {conversation}
                """;

            var summary = await CallGroqAsync(prompt);
            _logger.LogInformation("Summarization completed");
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing conversation");
            return "Unable to summarize conversation at this time.";
        }
    }

    public async Task<string> ComposeMessageAsync(
        string userPrompt, string? conversationContext)
    {
        try
        {
            _logger.LogInformation("Composing message for prompt: {Prompt}", userPrompt);

            var context = string.IsNullOrEmpty(conversationContext)
                ? "No previous context."
                : conversationContext;

            var prompt = $"""
                You are a helpful message composer for a chat app.
                
                Conversation context:
                {context}
                
                User wants to send this message (in their own words):
                "{userPrompt}"
                
                Write a polished, natural chat message based on what the user wants to say.
                Keep it conversational and appropriate for a chat app.
                Return ONLY the composed message, nothing else.
                Do not add quotes around the message.
                """;

            var composed = await CallGroqAsync(prompt);
            _logger.LogInformation("Message composed: {Result}", composed);
            return composed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error composing message");
            return userPrompt;
        }
    }
}