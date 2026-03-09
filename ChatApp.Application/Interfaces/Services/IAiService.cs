namespace ChatApp.Application.Interfaces.Services;

public interface IAiService
{
    Task<List<string>> GetSmartRepliesAsync(string lastMessage, string conversationContext);
    Task<string> TranslateMessageAsync(string message, string targetLanguage);
    Task<string> SummarizeConversationAsync(List<string> messages);
    Task<string> ComposeMessageAsync(string userPrompt, string conversationContext);
}