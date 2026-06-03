using HabiHamAIAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HabiHamAIAPI.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AppPermission> AppPermissions => Set<AppPermission>();
    public DbSet<AppRolePermission> AppRolePermissions => Set<AppRolePermission>();
    public DbSet<AppUserRoleAssignment> UserRoleAssignments => Set<AppUserRoleAssignment>();
    public DbSet<AiAssistant> AiAssistants => Set<AiAssistant>();
    public DbSet<AiAssistantFieldDefinition> AiAssistantFieldDefinitions => Set<AiAssistantFieldDefinition>();
    public DbSet<UserAiAssistantExtras> UserAiAssistantExtras => Set<UserAiAssistantExtras>();
    public DbSet<ChatDialog> ChatDialogs => Set<ChatDialog>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<UserWeightEntry> UserWeightEntries => Set<UserWeightEntry>();
    public DbSet<UserHabit> UserHabits => Set<UserHabit>();
    public DbSet<UserHabitCheckin> UserHabitCheckins => Set<UserHabitCheckin>();
    public DbSet<UserTodoItem> UserTodoItems => Set<UserTodoItem>();
    public DbSet<UserCategory> UserCategories => Set<UserCategory>();
    public DbSet<TelegramLinkToken> TelegramLinkTokens => Set<TelegramLinkToken>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<WorkoutSet> WorkoutSets => Set<WorkoutSet>();
    public DbSet<UserBikeActivity> UserBikeActivities => Set<UserBikeActivity>();
    public DbSet<UserBikeActivityTrackPoint> UserBikeActivityTrackPoints => Set<UserBikeActivityTrackPoint>();
    public DbSet<UserWeeklyTrainingReview> UserWeeklyTrainingReviews => Set<UserWeeklyTrainingReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasMany(x => x.RoleAssignments)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(x => x.TelegramChatId).HasColumnName("telegram_chat_id");
            entity.HasIndex(x => x.TelegramChatId).IsUnique();
            entity.HasOne(x => x.SelectedAiAssistant)
                .WithMany()
                .HasForeignKey(x => x.SelectedAiAssistantId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.WorkoutSessions)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.WeightEntries)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.BikeActivities)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.ToTable("app_roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(30).IsRequired();
            entity.Property(x => x.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(300);
            entity.Property(x => x.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.SortOrder);
        });

        modelBuilder.Entity<AppPermission>(entity =>
        {
            entity.ToTable("app_permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(x => x.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(300);
            entity.Property(x => x.Category).HasColumnName("category").HasMaxLength(30).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(x => x.IsSystem).HasColumnName("is_system").HasDefaultValue(true).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.SortOrder);
        });

        modelBuilder.Entity<AppRolePermission>(entity =>
        {
            entity.ToTable("app_role_permissions");
            entity.HasKey(x => new { x.RoleName, x.PermissionCode });
            entity.Property(x => x.RoleName).HasColumnName("role_name").HasMaxLength(30).IsRequired();
            entity.Property(x => x.PermissionCode).HasColumnName("permission_code").HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleName)
                .HasPrincipalKey(x => x.Name)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Permission)
                .WithMany()
                .HasForeignKey(x => x.PermissionCode)
                .HasPrincipalKey(x => x.Code)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppUserRoleAssignment>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(x => new { x.UserId, x.RoleName });
            entity.Property(x => x.RoleName)
                .HasColumnName("role")
                .HasMaxLength(30)
                .IsRequired();
            entity.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleName)
                .HasPrincipalKey(x => x.Name)
                .OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(x => x.AssistantCode).HasColumnName("assistant_code").HasMaxLength(64);
            entity.Property(x => x.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
            entity.HasIndex(x => x.AssistantCode).IsUnique();
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
            entity.Property(x => x.IsSystem).HasColumnName("is_system").HasDefaultValue(false).IsRequired();
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

        modelBuilder.Entity<UserWeeklyTrainingReview>(entity =>
        {
            entity.ToTable("user_weekly_training_reviews");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.AiAssistantId).HasColumnName("ai_assistant_id");
            entity.Property(x => x.PeriodFrom).HasColumnName("period_from").HasColumnType("date");
            entity.Property(x => x.PeriodTo).HasColumnName("period_to").HasColumnType("date");
            entity.Property(x => x.DataFingerprint).HasColumnName("data_fingerprint").HasMaxLength(256).IsRequired();
            entity.Property(x => x.Content).HasColumnName("content").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => new { x.UserId, x.PeriodFrom, x.PeriodTo }).IsUnique();
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
            entity.Property(x => x.AiAssistantId).HasColumnName("ai_assistant_id");
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            entity.HasOne(x => x.User)
                .WithMany(x => x.Dialogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AiAssistant)
                .WithMany()
                .HasForeignKey(x => x.AiAssistantId)
                .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<TelegramLinkToken>(entity =>
        {
            entity.ToTable("telegram_link_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TokenHashSha256Hex).HasColumnName("token_hash_sha256_hex").HasMaxLength(64).IsRequired();
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
            entity.Property(x => x.ConsumedAtUtc).HasColumnName("consumed_at_utc");
            entity.HasIndex(x => x.TokenHashSha256Hex).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ConsumedAtUtc });
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
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

        modelBuilder.Entity<UserWeightEntry>(entity =>
        {
            entity.ToTable("user_weight_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.Date).HasColumnName("date").HasColumnType("date").IsRequired();
            entity.Property(x => x.WeightKg).HasColumnName("weight_kg").HasPrecision(5, 2).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.UpdatedAtUtc });
        });

        modelBuilder.Entity<UserHabit>(entity =>
        {
            entity.ToTable("user_habits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.CategoryId).HasColumnName("category_id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            entity.Property(x => x.IsMastered).HasColumnName("is_mastered").HasDefaultValue(false).IsRequired();
            entity.Property(x => x.DaysToMaster).HasColumnName("days_to_master").HasDefaultValue(21).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

            entity.HasIndex(x => new { x.UserId, x.SortOrder });
            entity.HasIndex(x => new { x.UserId, x.IsActive });

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(x => x.Checkins)
                .WithOne(x => x.Habit)
                .HasForeignKey(x => x.HabitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserHabitCheckin>(entity =>
        {
            entity.ToTable("user_habit_checkins");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.HabitId).HasColumnName("habit_id").IsRequired();
            entity.Property(x => x.Date).HasColumnName("date").HasColumnType("date").IsRequired();
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue(UserHabitCheckinStatus.Done)
                .IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

            entity.HasIndex(x => new { x.HabitId, x.Date }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.Date });

            entity.HasOne(x => x.Habit)
                .WithMany(x => x.Checkins)
                .HasForeignKey(x => x.HabitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserTodoItem>(entity =>
        {
            entity.ToTable("user_todo_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(x => x.CategoryId).HasColumnName("category_id");
            entity.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(x => x.DueDate).HasColumnName("due_date").HasColumnType("date");
            entity.Property(x => x.DoneDate).HasColumnName("done_date").HasColumnType("date");
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

            entity.HasIndex(x => new { x.UserId, x.DoneDate });
            entity.HasIndex(x => new { x.UserId, x.DueDate });

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserCategory>(entity =>
        {
            entity.ToTable("user_categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(300);
            entity.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            entity.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.SortOrder);
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

        modelBuilder.Entity<UserBikeActivity>(entity =>
        {
            entity.ToTable("user_bike_activities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.Sport).HasColumnName("sport").HasMaxLength(80).IsRequired();
            entity.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
            entity.Property(x => x.StartTimeUtc).HasColumnName("start_time_utc").IsRequired();
            entity.Property(x => x.TotalSeconds).HasColumnName("total_seconds");
            entity.Property(x => x.DistanceMeters).HasColumnName("distance_meters");
            entity.Property(x => x.Calories).HasColumnName("calories");
            entity.Property(x => x.AverageHeartRateBpm).HasColumnName("avg_heart_rate_bpm");
            entity.Property(x => x.MaxHeartRateBpm).HasColumnName("max_heart_rate_bpm");
            entity.Property(x => x.Intensity).HasColumnName("intensity").HasMaxLength(40);
            entity.Property(x => x.TriggerMethod).HasColumnName("trigger_method").HasMaxLength(40);
            entity.Property(x => x.ImportedAtUtc).HasColumnName("imported_at_utc").IsRequired();
            entity.Property(x => x.TrackpointCount).HasColumnName("trackpoint_count").IsRequired();
            entity.Property(x => x.SourceFileKey).HasColumnName("source_file_key").HasMaxLength(500);
            entity.Property(x => x.SourceFileName).HasColumnName("source_file_name").HasMaxLength(260);
            entity.HasIndex(x => new { x.UserId, x.StartTimeUtc });
            entity.HasMany(x => x.TrackPoints)
                .WithOne(x => x.Activity)
                .HasForeignKey(x => x.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserBikeActivityTrackPoint>(entity =>
        {
            entity.ToTable("user_bike_activity_trackpoints");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ActivityId).HasColumnName("activity_id");
            entity.Property(x => x.OrderIndex).HasColumnName("order_no").IsRequired();
            entity.Property(x => x.TimeUtc).HasColumnName("time_utc").IsRequired();
            entity.Property(x => x.Latitude).HasColumnName("latitude");
            entity.Property(x => x.Longitude).HasColumnName("longitude");
            entity.Property(x => x.AltitudeMeters).HasColumnName("altitude_m");
            entity.Property(x => x.HeartRateBpm).HasColumnName("heart_rate_bpm");
            entity.Property(x => x.Cadence).HasColumnName("cadence");
            entity.Property(x => x.SpeedMetersPerSecond).HasColumnName("speed_m_s");
            entity.HasIndex(x => new { x.ActivityId, x.OrderIndex });
        });
    }
}
