namespace AiRunLedger.Api.Domain;

public sealed record CreateRunRequest(
    string? Model,
    long InputTokens,
    long OutputTokens,
    string? Status,
    DateTime? StartedAtUtc,
    long DurationMs,
    string? Source);

public static class RunRequestValidator
{
    public const long MaxTokens = 100_000_000;
    public const long MaxDurationMs = 24L * 60 * 60 * 1000;

    /// <summary>Returns a map of field name to message. Empty means valid.</summary>
    public static Dictionary<string, string[]> Validate(CreateRunRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Model))
            errors["model"] = ["Model is required."];
        else if (request.Model.Trim().Length > 100)
            errors["model"] = ["Model must be 100 characters or fewer."];

        if (request.InputTokens < 0 || request.InputTokens > MaxTokens)
            errors["inputTokens"] = [$"Input tokens must be between 0 and {MaxTokens}."];

        if (request.OutputTokens < 0 || request.OutputTokens > MaxTokens)
            errors["outputTokens"] = [$"Output tokens must be between 0 and {MaxTokens}."];

        if (request.DurationMs < 0 || request.DurationMs > MaxDurationMs)
            errors["durationMs"] = [$"Duration must be between 0 and {MaxDurationMs} ms."];

        if (!Enum.TryParse<RunStatus>(request.Status, ignoreCase: true, out _))
            errors["status"] = ["Status must be Succeeded or Failed."];

        if (!Enum.TryParse<DataSource>(request.Source, ignoreCase: true, out _))
            errors["source"] = ["Source must be Sample or Manual."];

        if (request.StartedAtUtc is { } started && started > DateTime.UtcNow.AddMinutes(5))
            errors["startedAtUtc"] = ["Start time cannot be in the future."];

        return errors;
    }
}
