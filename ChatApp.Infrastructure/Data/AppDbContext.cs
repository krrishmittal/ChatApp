using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<MessageReciept> MessageReciepts { get; set; } = null!;
    public DbSet<FileAttachment> FileAttachments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Rename Identity tables
        builder.Entity<User>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        // User
        builder.Entity<User>(e =>
        {
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
            e.Property(u => u.PasswordResetOtp).HasMaxLength(10);
            e.HasQueryFilter(u => !u.IsDeleted);
        });

        // RefreshToken
        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Token).IsRequired().HasMaxLength(500);
            e.HasOne(r => r.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(r => !r.User.IsDeleted);
        });

        // Conversation
        builder.Entity<Conversation>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.LastMessage)
             .WithMany()
             .HasForeignKey(c => c.LastMessageId)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasQueryFilter(c => !c.IsDeleted);
        });

        // ConversationParticipant
        builder.Entity<ConversationParticipant>(e =>
        {
            e.HasKey(cp => cp.Id);
            e.HasIndex(cp => new { cp.ConversationId, cp.UserId }).IsUnique();
            e.HasOne(cp => cp.Conversation)
             .WithMany(c => c.Participants)
             .HasForeignKey(cp => cp.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cp => cp.User)
             .WithMany(u => u.Conversations)
             .HasForeignKey(cp => cp.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(cp => !cp.IsDeleted);
        });

        // Message
        builder.Entity<Message>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Content).IsRequired().HasMaxLength(4000);
            e.HasOne(m => m.Conversation)
             .WithMany(c => c.Messages)
             .HasForeignKey(m => m.ConversationId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Sender)
             .WithMany(u => u.Messages)
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasQueryFilter(m => !m.IsDeleted);
        });

        // MessageReceipt
        builder.Entity<MessageReciept>(e =>
        {
            e.HasKey(mr => mr.Id);
            e.HasIndex(mr => new { mr.MessageId, mr.UserId }).IsUnique();
            e.Property(mr => mr.Status).HasConversion<int>();
            e.HasOne(mr => mr.Message)
             .WithMany(m => m.Reciepts)
             .HasForeignKey(mr => mr.MessageId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(mr => mr.User)
             .WithMany(u => u.MessageReceipts)
             .HasForeignKey(mr => mr.UserId)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasQueryFilter(mr => !mr.Message.IsDeleted);
        });

        // FileAttachment
        builder.Entity<FileAttachment>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.FileName).IsRequired().HasMaxLength(255);
            e.Property(f => f.FileUrl).IsRequired().HasMaxLength(500);
            e.Property(f => f.PublicId).IsRequired().HasMaxLength(500);
            e.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
            e.HasOne(f => f.Message)
             .WithMany(m => m.Attachments)
             .HasForeignKey(f => f.MessageId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(f => !f.Message.IsDeleted);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.NewGuid();
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}