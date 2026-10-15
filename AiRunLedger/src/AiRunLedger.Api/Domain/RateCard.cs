namespace AiRunLedger.Api.Domain;

/// <summary>Price in USD per 1 million tokens.</summary>
public sealed record ModelRate(decimal InputPerMillion, decimal OutputPerMillion);

/// <summary>
/// Saved rates per model. These are INVENTED demo rates, not real provider prices.
/// Replace them with numbers from your provider's pricing page if you want realistic estimates.
/// </summary>
public static class RateCard
{
    private static readonly Dictionary<string, ModelRate> Rates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["demo-small"] = new ModelRate(0.50m, 1.50m),
        ["demo-medium"] = new ModelRate(3.00m, 15.00m),
        ["demo-large"] = new ModelRate(15.00m, 75.00m),
    };

    public static IReadOnlyCollection<string> KnownModels => Rates.Keys.ToArray();

    /// <summary>Returns null when the model has no saved rate. Unknown is not the same as free.</summary>
    public static ModelRate? Find(string model) =>
        Rates.TryGetValue(model.Trim(), out var rate) ? rate : null;
}
