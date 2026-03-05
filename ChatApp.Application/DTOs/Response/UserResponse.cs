namespace ChatApp.Application.DTOs.Response;
public class UserResponse
{
    public Guid Id {  get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; }= null!;
    public string? ProfilePictureUrl { get; set; }
    public bool IsOnline { get; set; }  
}