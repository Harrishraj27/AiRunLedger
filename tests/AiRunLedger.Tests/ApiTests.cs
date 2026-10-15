using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace AiRunLedger.Tests;

/// <summary>Starts the real app against a throwaway SQLite file.</summary>
public sealed class TempDbFactory : WebApplicationFactory<Program>
{
    public string DbPath { get; }

    public TempDbFactory(string? dbPath = null) =>
        DbPath = dbPath ?? Path.Combine(Path.GetTempPath(), $"airunledger-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={DbPath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }
}

public class ApiTests : IDisposable
{
    private readonly TempDbFactory _factory = new();
    private readonly HttpClient _client;

    public ApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (File.Exists(_factory.DbPath)) File.Delete(_factory.DbPath);
    }

    private static object NewRun(string model = "demo-medium", long input = 1_000_000, long output = 1_000_000) => new
    {
        model, inputTokens = input, outputTokens = output, status = "Succeeded", durationMs = 1000, source = "Manual"
    };

    [Fact]
    public async Task PostRun_ReturnsCreated_WithEstimatedCostAndRateSnapshot()
    {
        var res = await _client.PostAsJsonAsync("/api/runs", NewRun());
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(18.0m, json.GetProperty("estimatedCostUsd").GetDecimal());
        Assert.Equal(3.0m, json.GetProperty("inputRatePerMillion").GetDecimal());
    }

    [Fact]
    public async Task PostRun_UnknownModel_SavesWithNullCost()
    {
        var res = await _client.PostAsJsonAsync("/api/runs", NewRun("mystery"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, json.GetProperty("estimatedCostUsd").ValueKind);
    }

    [Fact]
    public async Task PostRun_InvalidInput_Returns400WithFieldErrors()
    {
        var res = await _client.PostAsJsonAsync("/api/runs", NewRun(model: "", input: -5));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var text = await res.Content.ReadAsStringAsync();
        Assert.Contains("inputTokens", text);
        Assert.Contains("model", text);
    }

    [Fact]
    public async Task GetRun_ById_And_Missing()
    {
        var created = await (await _client.PostAsJsonAsync("/api/runs", NewRun())).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/runs/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/runs/99999")).StatusCode);
    }

    [Fact]
    public async Task ListAndSummary_ReflectSavedRuns()
    {
        await _client.PostAsJsonAsync("/api/runs", NewRun());
        await _client.PostAsJsonAsync("/api/runs", NewRun("mystery"));

        var runs = await _client.GetFromJsonAsync<JsonElement>("/api/runs");
        Assert.Equal(2, runs.GetArrayLength());

        var summary = await _client.GetFromJsonAsync<JsonElement>("/api/summary");
        Assert.Equal(2, summary.GetProperty("totalRuns").GetInt32());
        Assert.Equal(18.0m, summary.GetProperty("estimatedCostUsd").GetDecimal());
        Assert.Equal(1, summary.GetProperty("runsWithoutCost").GetInt32());
    }

    [Fact]
    public async Task LoadSamples_IsLabelledSample_AndOnlyLoadsOnce()
    {
        var first = await (await _client.PostAsync("/api/samples", null)).Content.ReadFromJsonAsync<JsonElement>();
        var second = await (await _client.PostAsync("/api/samples", null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(first.GetProperty("added").GetInt32() > 0);
        Assert.Equal(0, second.GetProperty("added").GetInt32());

        var summary = await _client.GetFromJsonAsync<JsonElement>("/api/summary");
        var sources = summary.GetProperty("bySource").EnumerateArray().Select(x => x.GetProperty("source").GetString()).ToList();
        Assert.Equal(["Sample"], sources);
    }

    [Fact]
    public async Task HomePage_IsServed()
    {
        var res = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("AI Run Ledger", await res.Content.ReadAsStringAsync());
    }
}

public class PersistenceTests
{
    [Fact]
    public async Task Runs_SurviveAppRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"airunledger-restart-{Guid.NewGuid():N}.db");
        try
        {
            using (var first = new TempDbFactory(path))
            using (var client = first.CreateClient())
            {
                var res = await client.PostAsJsonAsync("/api/runs", new
                {
                    model = "demo-small", inputTokens = 1000, outputTokens = 100,
                    status = "Succeeded", durationMs = 200, source = "Manual"
                });
                Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            }

            // A brand new app instance pointed at the same file.
            using (var second = new TempDbFactory(path))
            using (var client = second.CreateClient())
            {
                var runs = await client.GetFromJsonAsync<JsonElement>("/api/runs");
                Assert.Equal(1, runs.GetArrayLength());
                Assert.Equal("demo-small", runs[0].GetProperty("model").GetString());
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
