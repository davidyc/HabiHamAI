using HabiHamAIAPI.Data;
using HabiHamAIAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HabiHamAIDataMigrator;

internal sealed class UserMigrationPlan
{
    public required AppUser SourceUser { get; init; }
    public AppUser? TargetUser { get; init; }
    public bool ExistsInTarget => TargetUser is not null;
}

internal sealed class SqlServerToSqlServerMigrator
{
    private readonly AppDbContext _source;
    private readonly AppDbContext _target;
    private readonly HashSet<string> _usernames;
    private readonly List<UserMigrationPlan> _userPlans = [];
    private readonly Dictionary<Guid, Guid> _userIdMap = [];
    private readonly Dictionary<Guid, Guid> _assistantIdMap = [];
    private readonly Dictionary<Guid, Guid> _categoryIdMap = [];
    private HashSet<Guid> _sourceUserIds = [];

    public SqlServerToSqlServerMigrator(AppDbContext source, AppDbContext target, IEnumerable<string> usernames)
    {
        _source = source;
        _target = target;
        _usernames = usernames
            .Select(NormalizeUsername)
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_usernames.Count == 0)
        {
            throw new InvalidOperationException("Укажите хотя бы одного пользователя в MigrationSettings.UsernamesToMigrate.");
        }

        await VerifyUsersAsync(cancellationToken);

        await using IDbContextTransaction transaction = await _target.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await EnsureReferenceDataAsync(cancellationToken);
            await CreateUsersAsync(cancellationToken);
            await MigrateUserRolesAsync(cancellationToken);
            await MigrateUserAiAssistantExtrasAsync(cancellationToken);
            await MigrateChatDialogsAsync(cancellationToken);
            await MigrateTelegramLinkTokensAsync(cancellationToken);
            await MigrateWeightEntriesAsync(cancellationToken);
            await MigrateHabitsAsync(cancellationToken);
            await MigrateTodosAsync(cancellationToken);
            await MigrateWeeklyReviewsAsync(cancellationToken);
            await MigrateWorkoutsAsync(cancellationToken);
            await MigrateBikeActivitiesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            Console.WriteLine();
            Console.WriteLine("Миграция успешно завершена.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task VerifyUsersAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 1. Проверка пользователей");
        Console.WriteLine("-----------------------------");
        Console.WriteLine($"Запрошено: {string.Join(", ", _usernames.Order())}");
        Console.WriteLine();

        var sourceUsers = await _source.Users.AsNoTracking().ToListAsync(cancellationToken);
        var targetUsers = await _target.Users.AsNoTracking().ToListAsync(cancellationToken);
        var targetByUsername = targetUsers.ToDictionary(x => NormalizeUsername(x.Username), StringComparer.Ordinal);

        Console.WriteLine($"{"Username",-20} {"Источник Id",-38} {"В цели",-8} {"Цель Id"}");
        Console.WriteLine(new string('-', 90));

        foreach (var username in _usernames.Order())
        {
            var sourceUser = sourceUsers.FirstOrDefault(x => NormalizeUsername(x.Username) == username);
            if (sourceUser is null)
            {
                throw new InvalidOperationException($"Пользователь «{username}» не найден в базе-источнике.");
            }

            targetByUsername.TryGetValue(username, out var targetUser);
            _userPlans.Add(new UserMigrationPlan
            {
                SourceUser = sourceUser,
                TargetUser = targetUser
            });

            var existsLabel = targetUser is null ? "нет" : "да";
            var targetId = targetUser?.Id.ToString() ?? "—";
            Console.WriteLine($"{username,-20} {sourceUser.Id,-38} {existsLabel,-8} {targetId}");
        }

        _sourceUserIds = _userPlans.Select(x => x.SourceUser.Id).ToHashSet();
        var toCreate = _userPlans.Count(x => !x.ExistsInTarget);
        var existing = _userPlans.Count - toCreate;

        Console.WriteLine();
        Console.WriteLine($"Итого: {existing} уже в цели, {toCreate} будет создано.");
        Console.WriteLine();
    }

    private async Task CreateUsersAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 3. Создание пользователей");
        Console.WriteLine("-----------------------------");

        var created = 0;
        var updated = 0;

        foreach (var plan in _userPlans)
        {
            var sourceUser = plan.SourceUser;
            var normalized = NormalizeUsername(sourceUser.Username);

            if (plan.TargetUser is null)
            {
                var clone = CloneUser(sourceUser);
                clone.Username = normalized;
                clone.SelectedAiAssistantId = MapAssistantIdOrNull(sourceUser.SelectedAiAssistantId);
                _target.Users.Add(clone);
                _userIdMap[sourceUser.Id] = sourceUser.Id;
                Console.WriteLine($"  + создан: {normalized} (Id={sourceUser.Id})");
                created++;
            }
            else
            {
                var targetUser = await _target.Users
                    .FirstAsync(x => x.Id == plan.TargetUser.Id, cancellationToken);

                _userIdMap[sourceUser.Id] = targetUser.Id;
                ApplyUserProfile(sourceUser, targetUser);
                targetUser.SelectedAiAssistantId = MapAssistantIdOrNull(sourceUser.SelectedAiAssistantId);
                Console.WriteLine($"  = существует: {normalized} (Id={targetUser.Id})");
                updated++;
            }
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  создано: {created}, обновлено профилей: {updated}");
        Console.WriteLine();
    }

    private async Task EnsureReferenceDataAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 2. Справочники (ai_assistants, user_categories)");
        Console.WriteLine("---------------------------------------------------");

        var assistantIds = await CollectReferencedAssistantIdsAsync(cancellationToken);
        var sourceAssistants = await _source.AiAssistants
            .AsNoTracking()
            .Where(x => assistantIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var assistant in sourceAssistants)
        {
            var targetAssistant = await FindTargetAssistantAsync(assistant, cancellationToken);
            if (targetAssistant is null)
            {
                _target.AiAssistants.Add(CloneAssistant(assistant));
                await _target.SaveChangesAsync(cancellationToken);
                _assistantIdMap[assistant.Id] = assistant.Id;
                Console.WriteLine($"  + ai_assistant: {assistant.Name}");
            }
            else
            {
                _assistantIdMap[assistant.Id] = targetAssistant.Id;
                Console.WriteLine($"  = ai_assistant: {assistant.Name} → {targetAssistant.Id}");
            }

            await EnsureAssistantFieldDefinitionsAsync(assistant.Id, cancellationToken);
        }

        var categoryIds = await CollectReferencedCategoryIdsAsync(cancellationToken);
        if (categoryIds.Count > 0)
        {
            var sourceCategories = await _source.UserCategories
                .AsNoTracking()
                .Where(x => categoryIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            foreach (var category in sourceCategories)
            {
                var targetCategory = await _target.UserCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Name == category.Name, cancellationToken);

                if (targetCategory is null)
                {
                    _target.UserCategories.Add(CloneCategory(category));
                    await _target.SaveChangesAsync(cancellationToken);
                    _categoryIdMap[category.Id] = category.Id;
                    Console.WriteLine($"  + user_category: {category.Name}");
                }
                else
                {
                    _categoryIdMap[category.Id] = targetCategory.Id;
                }
            }
        }

        Console.WriteLine();
    }

    private async Task EnsureAssistantFieldDefinitionsAsync(Guid sourceAssistantId, CancellationToken cancellationToken)
    {
        var targetAssistantId = MapAssistantId(sourceAssistantId);
        var existingKeys = await _target.AiAssistantFieldDefinitions
            .AsNoTracking()
            .Where(x => x.AiAssistantId == targetAssistantId)
            .Select(x => x.FieldKey)
            .ToListAsync(cancellationToken);

        var sourceFields = await _source.AiAssistantFieldDefinitions
            .AsNoTracking()
            .Where(x => x.AiAssistantId == sourceAssistantId)
            .ToListAsync(cancellationToken);

        foreach (var field in sourceFields)
        {
            if (existingKeys.Contains(field.FieldKey))
            {
                continue;
            }

            _target.AiAssistantFieldDefinitions.Add(new AiAssistantFieldDefinition
            {
                Id = field.Id,
                AiAssistantId = targetAssistantId,
                FieldKey = field.FieldKey,
                Label = field.Label,
                FieldType = field.FieldType,
                SortOrder = field.SortOrder,
                IsRequired = field.IsRequired,
                IsSystem = field.IsSystem,
                CreatedAtUtc = field.CreatedAtUtc
            });
        }

        await _target.SaveChangesAsync(cancellationToken);
    }

    private async Task MigrateUserRolesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 4. user_roles");

        var roles = await _source.UserRoleAssignments
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var inserted = 0;
        foreach (var role in roles)
        {
            var userId = MapUserId(role.UserId);
            var exists = await _target.UserRoleAssignments
                .AnyAsync(x => x.UserId == userId && x.RoleName == role.RoleName, cancellationToken);
            if (exists)
            {
                continue;
            }

            if (!await _target.AppRoles.AnyAsync(x => x.Name == role.RoleName, cancellationToken))
            {
                Console.WriteLine($"  ! роль «{role.RoleName}» отсутствует в цели — пропуск");
                continue;
            }

            _target.UserRoleAssignments.Add(new AppUserRoleAssignment
            {
                UserId = userId,
                RoleName = role.RoleName
            });
            inserted++;
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  добавлено: {inserted}");
    }

    private async Task MigrateUserAiAssistantExtrasAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 5. user_ai_assistant_extras");

        var items = await _source.UserAiAssistantExtras
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var inserted = 0;
        foreach (var item in items)
        {
            var userId = MapUserId(item.UserId);
            var assistantId = MapAssistantId(item.AiAssistantId);
            if (await _target.UserAiAssistantExtras
                    .AnyAsync(x => x.UserId == userId && x.AiAssistantId == assistantId, cancellationToken))
            {
                continue;
            }

            _target.UserAiAssistantExtras.Add(new UserAiAssistantExtras
            {
                Id = item.Id,
                UserId = userId,
                AiAssistantId = assistantId,
                ValuesJson = item.ValuesJson
            });
            inserted++;
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  добавлено: {inserted}");
    }

    private async Task MigrateChatDialogsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 6. chat_dialogs, chat_messages");

        var dialogs = await _source.ChatDialogs
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var dialogIds = dialogs.Select(x => x.Id).ToList();
        var messages = dialogIds.Count == 0
            ? []
            : await _source.ChatMessages
                .AsNoTracking()
                .Where(x => dialogIds.Contains(x.DialogId))
                .ToListAsync(cancellationToken);

        var insertedDialogs = 0;
        var insertedMessages = 0;

        foreach (var dialog in dialogs)
        {
            if (await _target.ChatDialogs.AnyAsync(x => x.Id == dialog.Id, cancellationToken))
            {
                continue;
            }

            _target.ChatDialogs.Add(new ChatDialog
            {
                Id = dialog.Id,
                UserId = MapUserId(dialog.UserId),
                AiAssistantId = MapAssistantIdOrNull(dialog.AiAssistantId),
                Title = dialog.Title,
                CreatedAtUtc = dialog.CreatedAtUtc,
                UpdatedAtUtc = dialog.UpdatedAtUtc
            });
            insertedDialogs++;
        }

        await _target.SaveChangesAsync(cancellationToken);

        foreach (var message in messages)
        {
            if (await _target.ChatMessages.AnyAsync(x => x.Id == message.Id, cancellationToken))
            {
                continue;
            }

            _target.ChatMessages.Add(new ChatMessage
            {
                Id = message.Id,
                DialogId = message.DialogId,
                Role = message.Role,
                Content = message.Content,
                CreatedAtUtc = message.CreatedAtUtc
            });
            insertedMessages++;

            if (insertedMessages % MigrationSettings.BatchSize == 0)
            {
                await _target.SaveChangesAsync(cancellationToken);
            }
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  диалогов: {insertedDialogs}, сообщений: {insertedMessages}");
    }

    private async Task MigrateTelegramLinkTokensAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 7. telegram_link_tokens");

        var items = await _source.TelegramLinkTokens
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var inserted = 0;
        foreach (var item in items)
        {
            if (await _target.TelegramLinkTokens.AnyAsync(x => x.Id == item.Id, cancellationToken))
            {
                continue;
            }

            _target.TelegramLinkTokens.Add(new TelegramLinkToken
            {
                Id = item.Id,
                TokenHashSha256Hex = item.TokenHashSha256Hex,
                UserId = MapUserId(item.UserId),
                ExpiresAtUtc = item.ExpiresAtUtc,
                ConsumedAtUtc = item.ConsumedAtUtc
            });
            inserted++;
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  добавлено: {inserted}");
    }

    private async Task MigrateWeightEntriesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 8. user_weight_entries");

        var items = await _source.UserWeightEntries
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var inserted = 0;
        foreach (var item in items)
        {
            var userId = MapUserId(item.UserId);
            if (await _target.UserWeightEntries
                    .AnyAsync(x => x.UserId == userId && x.Date == item.Date, cancellationToken))
            {
                continue;
            }

            _target.UserWeightEntries.Add(new UserWeightEntry
            {
                Id = item.Id,
                UserId = userId,
                Date = item.Date,
                WeightKg = item.WeightKg,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            });
            inserted++;
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  добавлено: {inserted}");
    }

    private async Task MigrateHabitsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 9. user_habits, user_habit_checkins");

        var habits = await _source.UserHabits
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var habitIds = habits.Select(x => x.Id).ToList();
        var checkins = habitIds.Count == 0
            ? []
            : await _source.UserHabitCheckins
                .AsNoTracking()
                .Where(x => habitIds.Contains(x.HabitId))
                .ToListAsync(cancellationToken);

        var insertedHabits = 0;
        foreach (var habit in habits)
        {
            if (await _target.UserHabits.AnyAsync(x => x.Id == habit.Id, cancellationToken))
            {
                continue;
            }

            _target.UserHabits.Add(new UserHabit
            {
                Id = habit.Id,
                UserId = MapUserId(habit.UserId),
                CategoryId = MapCategoryIdOrNull(habit.CategoryId),
                Name = habit.Name,
                IsActive = habit.IsActive,
                IsMastered = habit.IsMastered,
                DaysToMaster = habit.DaysToMaster,
                SortOrder = habit.SortOrder,
                CreatedAtUtc = habit.CreatedAtUtc,
                UpdatedAtUtc = habit.UpdatedAtUtc
            });
            insertedHabits++;
        }

        await _target.SaveChangesAsync(cancellationToken);

        var insertedCheckins = 0;
        foreach (var checkin in checkins)
        {
            if (await _target.UserHabitCheckins.AnyAsync(x => x.Id == checkin.Id, cancellationToken))
            {
                continue;
            }

            _target.UserHabitCheckins.Add(new UserHabitCheckin
            {
                Id = checkin.Id,
                UserId = MapUserId(checkin.UserId),
                HabitId = checkin.HabitId,
                Date = checkin.Date,
                Status = checkin.Status,
                CreatedAtUtc = checkin.CreatedAtUtc,
                UpdatedAtUtc = checkin.UpdatedAtUtc
            });
            insertedCheckins++;
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  привычек: {insertedHabits}, чекинов: {insertedCheckins}");
    }

    private async Task MigrateTodosAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 10. user_todo_items");

        var items = await _source.UserTodoItems
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var inserted = 0;
        foreach (var item in items)
        {
            if (await _target.UserTodoItems.AnyAsync(x => x.Id == item.Id, cancellationToken))
            {
                continue;
            }

            _target.UserTodoItems.Add(new UserTodoItem
            {
                Id = item.Id,
                UserId = MapUserId(item.UserId),
                CategoryId = MapCategoryIdOrNull(item.CategoryId),
                Title = item.Title,
                DueDate = item.DueDate,
                DoneDate = item.DoneDate,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            });
            inserted++;
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  добавлено: {inserted}");
    }

    private async Task MigrateWeeklyReviewsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 11. user_weekly_training_reviews");

        var items = await _source.UserWeeklyTrainingReviews
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var inserted = 0;
        foreach (var item in items)
        {
            var userId = MapUserId(item.UserId);
            if (await _target.UserWeeklyTrainingReviews
                    .AnyAsync(x => x.UserId == userId && x.PeriodFrom == item.PeriodFrom && x.PeriodTo == item.PeriodTo, cancellationToken))
            {
                continue;
            }

            _target.UserWeeklyTrainingReviews.Add(new UserWeeklyTrainingReview
            {
                Id = item.Id,
                UserId = userId,
                AiAssistantId = MapAssistantId(item.AiAssistantId),
                PeriodFrom = item.PeriodFrom,
                PeriodTo = item.PeriodTo,
                DataFingerprint = item.DataFingerprint,
                Content = item.Content,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            });
            inserted++;
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  добавлено: {inserted}");
    }

    private async Task MigrateWorkoutsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 12. workout_sessions, workout_exercises, workout_sets");

        var sessions = await _source.WorkoutSessions
            .AsNoTracking()
            .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var insertedSessions = 0;
        var insertedExercises = 0;
        var insertedSets = 0;

        foreach (var session in sessions)
        {
            if (await _target.WorkoutSessions.AnyAsync(x => x.Id == session.Id, cancellationToken))
            {
                continue;
            }

            _target.WorkoutSessions.Add(new WorkoutSession
            {
                Id = session.Id,
                UserId = MapUserId(session.UserId),
                SessionCode = session.SessionCode,
                Date = session.Date,
                Day = session.Day,
                Notes = session.Notes,
                CreatedAtUtc = session.CreatedAtUtc,
                UpdatedAtUtc = session.UpdatedAtUtc,
                IsActive = session.IsActive
            });
            insertedSessions++;

            foreach (var exercise in session.Exercises)
            {
                if (await _target.WorkoutExercises.AnyAsync(x => x.Id == exercise.Id, cancellationToken))
                {
                    continue;
                }

                _target.WorkoutExercises.Add(new WorkoutExercise
                {
                    Id = exercise.Id,
                    SessionId = exercise.SessionId,
                    Name = exercise.Name,
                    Meta = exercise.Meta,
                    Order = exercise.Order
                });
                insertedExercises++;

                foreach (var set in exercise.Sets)
                {
                    if (await _target.WorkoutSets.AnyAsync(x => x.Id == set.Id, cancellationToken))
                    {
                        continue;
                    }

                    _target.WorkoutSets.Add(new WorkoutSet
                    {
                        Id = set.Id,
                        ExerciseId = set.ExerciseId,
                        Weight = set.Weight,
                        Reps = set.Reps,
                        Rpe = set.Rpe,
                        Order = set.Order
                    });
                    insertedSets++;
                }
            }
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  сессий: {insertedSessions}, упражнений: {insertedExercises}, подходов: {insertedSets}");
    }

    private async Task MigrateBikeActivitiesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Шаг 13. user_bike_activities, user_bike_activity_trackpoints");

        var activities = await _source.UserBikeActivities
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var activityIds = activities.Select(x => x.Id).ToList();
        var trackPoints = activityIds.Count == 0
            ? []
            : await _source.UserBikeActivityTrackPoints
                .AsNoTracking()
                .Where(x => activityIds.Contains(x.ActivityId))
                .OrderBy(x => x.ActivityId)
                .ThenBy(x => x.OrderIndex)
                .ToListAsync(cancellationToken);

        var insertedActivities = 0;
        foreach (var activity in activities)
        {
            if (await _target.UserBikeActivities.AnyAsync(x => x.Id == activity.Id, cancellationToken))
            {
                continue;
            }

            _target.UserBikeActivities.Add(new UserBikeActivity
            {
                Id = activity.Id,
                UserId = MapUserId(activity.UserId),
                Sport = activity.Sport,
                Notes = activity.Notes,
                StartTimeUtc = activity.StartTimeUtc,
                TotalSeconds = activity.TotalSeconds,
                DistanceMeters = activity.DistanceMeters,
                Calories = activity.Calories,
                AverageHeartRateBpm = activity.AverageHeartRateBpm,
                MaxHeartRateBpm = activity.MaxHeartRateBpm,
                Intensity = activity.Intensity,
                TriggerMethod = activity.TriggerMethod,
                ImportedAtUtc = activity.ImportedAtUtc,
                TrackpointCount = activity.TrackpointCount,
                SourceFileKey = activity.SourceFileKey,
                SourceFileName = activity.SourceFileName
            });
            insertedActivities++;
        }

        await _target.SaveChangesAsync(cancellationToken);

        var existingTrackPointIds = activityIds.Count == 0
            ? []
            : await _target.UserBikeActivityTrackPoints
                .AsNoTracking()
                .Where(x => activityIds.Contains(x.ActivityId))
                .Select(x => x.Id)
                .ToHashSetAsync(cancellationToken);

        var insertedTrackPoints = 0;
        var pending = 0;
        foreach (var point in trackPoints)
        {
            if (existingTrackPointIds.Contains(point.Id))
            {
                continue;
            }

            _target.UserBikeActivityTrackPoints.Add(new UserBikeActivityTrackPoint
            {
                Id = point.Id,
                ActivityId = point.ActivityId,
                OrderIndex = point.OrderIndex,
                TimeUtc = point.TimeUtc,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                AltitudeMeters = point.AltitudeMeters,
                HeartRateBpm = point.HeartRateBpm,
                Cadence = point.Cadence,
                SpeedMetersPerSecond = point.SpeedMetersPerSecond
            });
            insertedTrackPoints++;
            pending++;

            if (pending >= MigrationSettings.BatchSize)
            {
                await _target.SaveChangesAsync(cancellationToken);
                pending = 0;
            }
        }

        await _target.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"  заездов: {insertedActivities}, трекпоинтов: {insertedTrackPoints}");
    }

    private async Task<HashSet<Guid>> CollectReferencedAssistantIdsAsync(CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>();

        ids.UnionWith(await _source.Users
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.Id) && x.SelectedAiAssistantId != null)
            .Select(x => x.SelectedAiAssistantId!.Value)
            .ToListAsync(cancellationToken));

        ids.UnionWith(await _source.ChatDialogs
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId) && x.AiAssistantId != null)
            .Select(x => x.AiAssistantId!.Value)
            .ToListAsync(cancellationToken));

        ids.UnionWith(await _source.UserAiAssistantExtras
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .Select(x => x.AiAssistantId)
            .ToListAsync(cancellationToken));

        ids.UnionWith(await _source.UserWeeklyTrainingReviews
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId))
            .Select(x => x.AiAssistantId)
            .ToListAsync(cancellationToken));

        return ids;
    }

    private async Task<HashSet<Guid>> CollectReferencedCategoryIdsAsync(CancellationToken cancellationToken)
    {
        var habitCategoryIds = await _source.UserHabits
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId) && x.CategoryId != null)
            .Select(x => x.CategoryId!.Value)
            .ToListAsync(cancellationToken);

        var todoCategoryIds = await _source.UserTodoItems
            .AsNoTracking()
            .Where(x => _sourceUserIds.Contains(x.UserId) && x.CategoryId != null)
            .Select(x => x.CategoryId!.Value)
            .ToListAsync(cancellationToken);

        return habitCategoryIds.Concat(todoCategoryIds).ToHashSet();
    }

    private async Task<AiAssistant?> FindTargetAssistantAsync(AiAssistant source, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(source.AssistantCode))
        {
            var byCode = await _target.AiAssistants
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AssistantCode == source.AssistantCode, cancellationToken);
            if (byCode is not null)
            {
                return byCode;
            }
        }

        return await _target.AiAssistants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == source.Id, cancellationToken)
            ?? await _target.AiAssistants
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == source.Name, cancellationToken);
    }

    private Guid MapUserId(Guid sourceUserId) =>
        _userIdMap.TryGetValue(sourceUserId, out var targetId) ? targetId : sourceUserId;

    private Guid MapAssistantId(Guid sourceAssistantId) =>
        _assistantIdMap.TryGetValue(sourceAssistantId, out var targetId) ? targetId : sourceAssistantId;

    private Guid? MapAssistantIdOrNull(Guid? sourceAssistantId) =>
        sourceAssistantId is null ? null : MapAssistantId(sourceAssistantId.Value);

    private Guid? MapCategoryIdOrNull(Guid? sourceCategoryId) =>
        sourceCategoryId is null
            ? null
            : _categoryIdMap.TryGetValue(sourceCategoryId.Value, out var targetId)
                ? targetId
                : sourceCategoryId;

    private static string NormalizeUsername(string username) =>
        (username ?? string.Empty).Trim().ToLowerInvariant();

    private static AppUser CloneUser(AppUser source) => new()
    {
        Id = source.Id,
        TelegramChatId = source.TelegramChatId,
        Username = source.Username,
        PasswordHash = source.PasswordHash,
        CreatedAtUtc = source.CreatedAtUtc,
        BirthDate = source.BirthDate,
        HeightCm = source.HeightCm,
        WeightKg = source.WeightKg,
        Phone = source.Phone,
        City = source.City,
        About = source.About,
        FirstName = source.FirstName,
        LastName = source.LastName,
        AiSummary = source.AiSummary,
        SelectedAiAssistantId = source.SelectedAiAssistantId
    };

    private static void ApplyUserProfile(AppUser source, AppUser target)
    {
        target.TelegramChatId = source.TelegramChatId;
        target.PasswordHash = source.PasswordHash;
        target.BirthDate = source.BirthDate;
        target.HeightCm = source.HeightCm;
        target.WeightKg = source.WeightKg;
        target.Phone = source.Phone;
        target.City = source.City;
        target.About = source.About;
        target.FirstName = source.FirstName;
        target.LastName = source.LastName;
        target.AiSummary = source.AiSummary;
    }

    private static AiAssistant CloneAssistant(AiAssistant source) => new()
    {
        Id = source.Id,
        AssistantCode = source.AssistantCode,
        Name = source.Name,
        Description = source.Description,
        SystemPrompt = source.SystemPrompt,
        SettingsJson = source.SettingsJson,
        SortOrder = source.SortOrder,
        IsActive = source.IsActive,
        IsSystem = source.IsSystem,
        CreatedAtUtc = source.CreatedAtUtc
    };

    private static UserCategory CloneCategory(UserCategory source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
        IsActive = source.IsActive,
        SortOrder = source.SortOrder,
        CreatedAtUtc = source.CreatedAtUtc,
        UpdatedAtUtc = source.UpdatedAtUtc
    };
}
