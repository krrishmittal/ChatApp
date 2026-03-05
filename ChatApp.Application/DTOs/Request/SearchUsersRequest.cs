namespace ChatApp.Application.DTOs.Request;
public class SearchUsersRequest
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}