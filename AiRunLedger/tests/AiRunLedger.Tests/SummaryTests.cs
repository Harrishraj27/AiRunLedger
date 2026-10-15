using AiRunLedger.Api.Domain;

namespace AiRunLedger.Tests;

public class SummaryTests
{
    private static RunRecord Run(DataSource src, decimal? cost, RunStatus st = RunStatus.Succeeded) =>
        new() { Model = "m", InputTokens = 10, OutputTokens = 5, Source = src, EstimatedCostUsd = cost, Status = st };

    [Fact]
    public void UnknownCost_IsCountedSeparately_NotAddedAsZeroCostRun()
    {
        var s = RunService.Summarize([Run(DataSource.Manual, 1.5m), Run(DataSource.Manual, null)]);
        Assert.Equal(1.5m, s.EstimatedCostUsd);
        Assert.Equal(1, s.RunsWithoutCost);
        Assert.Equal(2, s.TotalRuns);
    }

    [Fact]
    public void SampleAndManual_AreTotalledSeparately()
    {
        var s = RunService.Summarize([Run(DataSource.Sample, 1m), Run(DataSource.Manual, 2m), Run(DataSource.Manual, 3m)]);
        Assert.Equal(1m, s.BySource.Single(x => x.Source == "Sample").EstimatedCostUsd);
        Assert.Equal(5m, s.BySource.Single(x => x.Source == "Manual").EstimatedCostUsd);
        Assert.Equal(6m, s.EstimatedCostUsd);
    }

    [Fact]
    public void FailedRuns_AreCounted()
    {
        var s = RunService.Summarize([Run(DataSource.Manual, 1m, RunStatus.Failed), Run(DataSource.Manual, 1m)]);
        Assert.Equal(1, s.FailedRuns);
    }

    [Fact]
    public void EmptyLedger_IsAllZero()
    {
        var s = RunService.Summarize([]);
        Assert.Equal(0, s.TotalRuns);
        Assert.Equal(0m, s.EstimatedCostUsd);
    }
}
