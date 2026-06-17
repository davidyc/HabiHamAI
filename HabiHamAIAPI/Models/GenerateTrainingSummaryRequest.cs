namespace HabiHamAIAPI.Models;

public sealed class GenerateTrainingSummaryRequest
{
    /// <summary>Модель LLM. Если не задана — модель ассистента «Тренер» или значение по умолчанию.</summary>
    public string? Model { get; set; }
}
