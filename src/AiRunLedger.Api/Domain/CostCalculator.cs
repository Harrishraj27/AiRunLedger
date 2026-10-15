namespace AiRunLedger.Api.Domain;

public static class CostCalculator
{
    private const decimal TokensPerMillion = 1_000_000m;

    /// <summary>Estimated cost in USD. Rounded to 6 decimal places (tiny runs cost fractions of a cent).</summary>
    public static decimal Estimate(long inputTokens, long outputTokens, ModelRate rate)
    {
        if (inputTokens < 0) throw new ArgumentOutOfRangeException(nameof(inputTokens));
        if (outputTokens < 0) throw new ArgumentOutOfRangeException(nameof(outputTokens));

        var cost = inputTokens / TokensPerMillion * rate.InputPerMillion
                 + outputTokens / TokensPerMillion * rate.OutputPerMillion;
        return Math.Round(cost, 6, MidpointRounding.AwayFromZero);
    }
}
