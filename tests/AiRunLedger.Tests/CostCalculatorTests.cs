using AiRunLedger.Api.Domain;

namespace AiRunLedger.Tests;

public class CostCalculatorTests
{
    [Fact]
    public void Estimate_UsesInputAndOutputRates()
    {
        var rate = new ModelRate(3.00m, 15.00m);
        // 1M input = $3, 1M output = $15
        Assert.Equal(18.00m, CostCalculator.Estimate(1_000_000, 1_000_000, rate));
    }

    [Fact]
    public void Estimate_ZeroTokens_IsZero()
    {
        Assert.Equal(0m, CostCalculator.Estimate(0, 0, new ModelRate(3m, 15m)));
    }

    [Fact]
    public void Estimate_SmallRun_KeepsFractionsOfACent()
    {
        // 1200 in * 0.50/M = 0.0006 ; 300 out * 1.50/M = 0.00045 ; total 0.00105
        Assert.Equal(0.00105m, CostCalculator.Estimate(1200, 300, new ModelRate(0.50m, 1.50m)));
    }

    [Fact]
    public void Estimate_NegativeTokens_Throws()
    {
        var rate = new ModelRate(1m, 1m);
        Assert.Throws<ArgumentOutOfRangeException>(() => CostCalculator.Estimate(-1, 0, rate));
        Assert.Throws<ArgumentOutOfRangeException>(() => CostCalculator.Estimate(0, -1, rate));
    }

    [Fact]
    public void RateCard_UnknownModel_ReturnsNull_NotZero()
    {
        Assert.Null(RateCard.Find("not-a-model"));
    }

    [Fact]
    public void RateCard_IsCaseInsensitive()
    {
        Assert.NotNull(RateCard.Find("DEMO-SMALL"));
    }
}
