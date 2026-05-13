namespace HabiHamAIAPI.Services;

public interface IUserWeightRecordingService
{
    Task RecordWeightTrackerEntryAsync(Guid userId, DateOnly date, decimal weightKg, CancellationToken cancellationToken);
}
