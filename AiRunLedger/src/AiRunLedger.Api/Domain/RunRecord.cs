namespace AiRunLedger.Api.Domain;

public class RunRecord
{
    public int Id { get; set; }
    public string Model { get; set; } = "";
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public RunStatus Status { get; set; }
    public DataSource Source { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public long DurationMs { get; set; }

    // Rate snapshot: the rates used at save time, so old runs keep their original estimate
    // even if the rate card changes later. All three are null when the model has no saved rate.
    public decimal? InputRatePerMillion { get; set; }
    public decimal? OutputRatePerMillion { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
}
