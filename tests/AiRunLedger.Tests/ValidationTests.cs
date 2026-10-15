using AiRunLedger.Api.Domain;

namespace AiRunLedger.Tests;

public class ValidationTests
{
    private static CreateRunRequest Valid() =>
        new("demo-small", 100, 50, "Succeeded", DateTime.UtcNow.AddMinutes(-1), 500, "Manual");

    [Fact]
    public void ValidRequest_HasNoErrors() => Assert.Empty(RunRequestValidator.Validate(Valid()));

    [Fact]
    public void MissingModel_IsRejected() =>
        Assert.Contains("model", RunRequestValidator.Validate(Valid() with { Model = " " }).Keys);

    [Fact]
    public void NegativeTokens_AreRejected()
    {
        var errors = RunRequestValidator.Validate(Valid() with { InputTokens = -1, OutputTokens = -5 });
        Assert.Contains("inputTokens", errors.Keys);
        Assert.Contains("outputTokens", errors.Keys);
    }

    [Fact]
    public void NegativeDuration_IsRejected() =>
        Assert.Contains("durationMs", RunRequestValidator.Validate(Valid() with { DurationMs = -1 }).Keys);

    [Fact]
    public void BadStatus_IsRejected() =>
        Assert.Contains("status", RunRequestValidator.Validate(Valid() with { Status = "Maybe" }).Keys);

    [Fact]
    public void BadSource_IsRejected() =>
        Assert.Contains("source", RunRequestValidator.Validate(Valid() with { Source = "Real" }).Keys);

    [Fact]
    public void FutureStartTime_IsRejected() =>
        Assert.Contains("startedAtUtc",
            RunRequestValidator.Validate(Valid() with { StartedAtUtc = DateTime.UtcNow.AddDays(1) }).Keys);
}
