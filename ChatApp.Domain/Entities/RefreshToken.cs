namespace ChatApp.Domain.Entities;

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}