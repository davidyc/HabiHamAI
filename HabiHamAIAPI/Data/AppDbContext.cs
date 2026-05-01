using HabiHamAIAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ChatDialog> ChatDialogs => Set<ChatDialog>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(AppUserRole.User)
                .IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.BirthDate).HasColumnType("date");
            entity.Property(x => x.HeightCm).HasPrecision(5, 2);
            entity.Property(x => x.WeightKg).HasPrecision(5, 2);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.City).HasMaxLength(120);
            entity.Property(x => x.About).HasMaxLength(500);
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<ChatDialog>(entity =>
        {
            entity.ToTable("chat_dialogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            entity.HasOne(x => x.User)
                .WithMany(x => x.Dialogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.DialogId).HasColumnName("dialog_id");
            entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(30).IsRequired();
            entity.Property(x => x.Content).HasColumnName("content").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.HasIndex(x => new { x.DialogId, x.CreatedAtUtc });
            entity.HasOne(x => x.Dialog)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.DialogId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
