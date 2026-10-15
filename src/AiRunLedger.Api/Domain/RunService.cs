using AiRunLedger.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AiRunLedger.Api.Domain;

public sealed record SourceSummary(string Source, int Runs, long InputTokens, long OutputTokens, decimal EstimatedCostUsd, int RunsWithoutCost);

public sealed record LedgerSummary(
    int TotalRuns,
    int FailedRuns,
    long InputTokens,
    long OutputTokens,
    decimal EstimatedCostUsd,
    int RunsWithoutCost,
    string Note,
    IReadOnlyList<SourceSummary> BySource);

public class RunService(LedgerDbContext db)
{
    public async Task<RunRecord> AddAsync(CreateRunRequest request)
    {
        var rate = RateCard.Find(request.Model!);
        var record = new RunRecord
        {
            Model = request.Model!.Trim(),
            InputTokens = request.InputTokens,
            OutputTokens = request.OutputTokens,
            Status = Enum.Parse<RunStatus>(request.Status!, true),
            Source = Enum.Parse<DataSource>(request.Source!, true),
            StartedAtUtc = DateTime.SpecifyKind(request.StartedAtUtc ?? DateTime.UtcNow, DateTimeKind.Utc),
            DurationMs = request.DurationMs,
        };

        if (rate is not null)
        {
            record.InputRatePerMillion = rate.InputPerMillion;
            record.OutputRatePerMillion = rate.OutputPerMillion;
            record.EstimatedCostUsd = CostCalculator.Estimate(record.InputTokens, record.OutputTokens, rate);
        }

        db.Runs.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    public async Task<List<RunRecord>> ListAsync()
    {
        var all = await db.Runs.AsNoTracking().ToListAsync();
        return all.OrderByDescending(r => r.StartedAtUtc).ThenByDescending(r => r.Id).ToList();
    }

    public Task<RunRecord?> GetAsync(int id) => db.Runs.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    public async Task<LedgerSummary> SummarizeAsync()
    {
        var runs = await db.Runs.AsNoTracking().ToListAsync();
        return Summarize(runs);
    }

    public static LedgerSummary Summarize(IReadOnlyCollection<RunRecord> runs)
    {
        static SourceSummary ForGroup(string name, IEnumerable<RunRecord> g)
        {
            var list = g.ToList();
            return new SourceSummary(
                name, list.Count,
                list.Sum(r => r.InputTokens), list.Sum(r => r.OutputTokens),
                list.Sum(r => r.EstimatedCostUsd ?? 0m),
                list.Count(r => r.EstimatedCostUsd is null));
        }

        var bySource = runs.GroupBy(r => r.Source.ToString())
            .OrderBy(g => g.Key)
            .Select(g => ForGroup(g.Key, g))
            .ToList();

        return new LedgerSummary(
            runs.Count,
            runs.Count(r => r.Status == RunStatus.Failed),
            runs.Sum(r => r.InputTokens),
            runs.Sum(r => r.OutputTokens),
            runs.Sum(r => r.EstimatedCostUsd ?? 0m),
            runs.Count(r => r.EstimatedCostUsd is null),
            "Estimates from saved demo rates, not provider bills. Runs without a known rate are counted in runsWithoutCost and are NOT treated as free.",
            bySource);
    }

    public async Task<int> LoadSamplesAsync()
    {
        // Only load once, so pressing the button twice does not double the totals.
        if (await db.Runs.AnyAsync(r => r.Source == DataSource.Sample)) return 0;

        var now = DateTime.UtcNow;
        var samples = new[]
        {
            new CreateRunRequest("demo-small", 1200, 300, "Succeeded", now.AddHours(-6), 850, "Sample"),
            new CreateRunRequest("demo-medium", 8500, 1200, "Succeeded", now.AddHours(-5), 4200, "Sample"),
            new CreateRunRequest("demo-large", 20000, 3500, "Succeeded", now.AddHours(-4), 15000, "Sample"),
            new CreateRunRequest("demo-small", 900, 0, "Failed", now.AddHours(-3), 300, "Sample"),
            new CreateRunRequest("demo-medium", 4000, 800, "Succeeded", now.AddHours(-2), 2100, "Sample"),
            new CreateRunRequest("mystery-model", 5000, 1000, "Succeeded", now.AddHours(-1), 1800, "Sample"),
        };
        foreach (var s in samples) await AddAsync(s);
        return samples.Length;
    }
}
