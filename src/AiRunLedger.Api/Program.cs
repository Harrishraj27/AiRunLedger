using AiRunLedger.Api.Data;
using AiRunLedger.Api.Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=airunledger.db";
builder.Services.AddDbContext<LedgerDbContext>(o => o.UseSqlite(connectionString));
builder.Services.AddScoped<RunService>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

// Create the database file and tables on startup. Good enough for a learning project;
// a real service would use EF migrations.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LedgerDbContext>().Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/models", () => RateCard.KnownModels);

api.MapGet("/runs", async (RunService svc) => await svc.ListAsync());

api.MapGet("/runs/{id:int}", async (int id, RunService svc) =>
    await svc.GetAsync(id) is { } run ? Results.Ok(run) : Results.NotFound());

api.MapPost("/runs", async (CreateRunRequest request, RunService svc) =>
{
    var errors = RunRequestValidator.Validate(request);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var run = await svc.AddAsync(request);
    return Results.Created($"/api/runs/{run.Id}", run);
});

api.MapGet("/summary", async (RunService svc) => await svc.SummarizeAsync());

api.MapPost("/samples", async (RunService svc) =>
{
    var added = await svc.LoadSamplesAsync();
    return Results.Ok(new { added });
});

app.Run();

// Lets the integration tests start the app with WebApplicationFactory<Program>.
public partial class Program;
