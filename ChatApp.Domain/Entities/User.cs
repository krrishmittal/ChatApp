using ChatApp.Domain.Enums;
using Microsoft.AspNetCore.Identity;    
namespace ChatApp.Domain.Entities;
public class User : IdentityUser<Guid>
{
    public string FullName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsGoogleAccount { get; set; }
    public string? PasswordResetOtp { get; set; }
    public DateTime? PassordResetOtpExpiry {  get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? FcmToken { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public bool IsScheduled => ScheduledAt.HasValue && ScheduledAt > DateTime.UtcNow;

    //navigation properties
    public ICollection<ConversationParticipant> Conversations { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message>Messages { get; set; } = new List<Message>();
    public ICollection<MessageReciept> MessageReceipts { get; set;} = new List<MessageReciept>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

}
