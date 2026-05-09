using HabiHamAIAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AiAssistant> AiAssistants => Set<AiAssistant>();
    public DbSet<AiAssistantFieldDefinition> AiAssistantFieldDefinitions => Set<AiAssistantFieldDefinition>();
    public DbSet<UserAiAssistantExtras> UserAiAssistantExtras => Set<UserAiAssistantExtras>();
    public DbSet<ChatDialog> ChatDialogs => Set<ChatDialog>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<WorkoutSet> WorkoutSets => Set<WorkoutSet>();

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
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.AiSummary).HasMaxLength(8000);
            entity.Property(x => x.SelectedAiAssistantId).HasColumnName("selected_ai_assistant_id");
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasOne(x => x.SelectedAiAssistant)
                .WithMany()
                .HasForeignKey(x => x.SelectedAiAssistantId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.WorkoutSessions)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiAssistant>(entity =>
        {
            entity.ToTable("ai_assistants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(x => x.SystemPrompt).HasColumnName("system_prompt").IsRequired();
            entity.Property(x => x.SettingsJson).HasColumnName("settings_json");
            entity.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.HasIndex(x => x.SortOrder);
        });

        modelBuilder.Entity<AiAssistantFieldDefinition>(entity =>
        {
            entity.ToTable("ai_assistant_field_definitions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.AiAssistantId).HasColumnName("ai_assistant_id");
            entity.Property(x => x.FieldKey).HasColumnName("field_key").HasMaxLength(80).IsRequired();
            entity.Property(x => x.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
            entity.Property(x => x.FieldType).HasColumnName("field_type").HasMaxLength(30).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(x => x.IsRequired).HasColumnName("is_required").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.HasIndex(x => new { x.AiAssistantId, x.FieldKey }).IsUnique();
            entity.HasIndex(x => new { x.AiAssistantId, x.SortOrder });
            entity.HasOne(x => x.AiAssistant)
                .WithMany()
                .HasForeignKey(x => x.AiAssistantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAiAssistantExtras>(entity =>
        {
            entity.ToTable("user_ai_assistant_extras");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.AiAssistantId).HasColumnName("ai_assistant_id");
            entity.Property(x => x.ValuesJson).HasColumnName("values_json").IsRequired();
            entity.HasIndex(x => new { x.UserId, x.AiAssistantId }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AiAssistant)
                .WithMany()
                .HasForeignKey(x => x.AiAssistantId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.ToTable("workout_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.SessionCode).HasColumnName("session_code").HasMaxLength(200).IsRequired();
            entity.Property(x => x.Date).HasColumnName("date").HasColumnType("date").IsRequired();
            entity.Property(x => x.Day).HasColumnName("day").HasMaxLength(120).IsRequired();
            entity.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(false).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.Date });
            entity.HasIndex(x => new { x.UserId, x.SessionCode }).IsUnique();
            entity.HasMany(x => x.Exercises)
                .WithOne(x => x.Session)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutExercise>(entity =>
        {
            entity.ToTable("workout_exercises");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.SessionId).HasColumnName("session_id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            entity.Property(x => x.Meta).HasColumnName("meta").HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Order).HasColumnName("order_no").IsRequired();
            entity.HasIndex(x => new { x.SessionId, x.Order });
            entity.HasMany(x => x.Sets)
                .WithOne(x => x.Exercise)
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutSet>(entity =>
        {
            entity.ToTable("workout_sets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ExerciseId).HasColumnName("exercise_id");
            entity.Property(x => x.Weight).HasColumnName("weight").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Reps).HasColumnName("reps").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Rpe).HasColumnName("rpe").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Order).HasColumnName("order_no").IsRequired();
            entity.HasIndex(x => new { x.ExerciseId, x.Order });
        });
    }
}
