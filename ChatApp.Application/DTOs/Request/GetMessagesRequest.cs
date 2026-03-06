namespace ChatApp.Application.DTOs.Request;

public class GetMessagesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}