using DshLauncher;
using Xunit;

namespace DshLauncher.Tests;

/// <summary>BalanceService tests: payload parsing and notification formatting
/// (no network — parsing is exercised directly).</summary>
public class BalanceServiceTests
{
    [Fact]
    public void ParseBalance_ValidPayload_ExtractsEntries()
    {
        var json = """
        {
          "is_available": true,
          "balance_infos": [
            { "currency": "CNY", "total_balance": "100.50", "granted_balance": "10.00", "topped_up_balance": "90.50" }
          ]
        }
        """;
        var result = BalanceService.ParseBalance(json);
        Assert.True(result.Ok);
        var b = Assert.Single(result.Balances);
        Assert.Equal("CNY", b.Currency);
        Assert.Equal(100.50m, b.Total);
        Assert.Equal(10.00m, b.Granted);
        Assert.Equal(90.50m, b.ToppedUp);
    }

    [Fact]
    public void ParseBalance_MultipleCurrencies_AllReturned()
    {
        var json = """
        {
          "balance_infos": [
            { "currency": "CNY", "total_balance": "1.5", "granted_balance": "0", "topped_up_balance": "1.5" },
            { "currency": "USD", "total_balance": "0.2", "granted_balance": "0.2", "topped_up_balance": "0" }
          ]
        }
        """;
        var result = BalanceService.ParseBalance(json);
        Assert.True(result.Ok);
        Assert.Equal(2, result.Balances.Count);
        Assert.Equal("USD", result.Balances[1].Currency);
    }

    [Fact]
    public void ParseBalance_EmptyInfos_OkButNoBalances()
    {
        var result = BalanceService.ParseBalance("{\"is_available\":false,\"balance_infos\":[]}");
        Assert.True(result.Ok);
        Assert.Empty(result.Balances);
    }

    [Fact]
    public void ParseBalance_InvalidJson_ReturnsFail()
    {
        var result = BalanceService.ParseBalance("not json");
        Assert.False(result.Ok);
        Assert.NotNull(result.Error);
        Assert.Empty(result.Balances);
    }

    [Fact]
    public void ParseBalance_MissingFields_DefaultToZero()
    {
        var json = "{\"balance_infos\":[{\"currency\":\"CNY\"}]}";
        var result = BalanceService.ParseBalance(json);
        var b = Assert.Single(result.Balances);
        Assert.Equal(0m, b.Total);
        Assert.Equal(0m, b.Granted);
        Assert.Equal(0m, b.ToppedUp);
    }

    [Fact]
    public void FormatForNotification_OkWithGranted_ShowsBreakdown()
    {
        var result = new BalanceResult(true, new[]
        {
            new BalanceEntry("CNY", 100.5m, 10m, 90.5m),
        }, null);
        var text = BalanceService.FormatForNotification(result);
        Assert.Contains("100.5", text);
        Assert.Contains("赠金", text);
    }

    [Fact]
    public void FormatForNotification_NoKey_HasGuidance()
    {
        var text = BalanceService.FormatForNotification(BalanceResult.Fail("no-key"));
        Assert.Contains("DEEPSEEK_API_KEY", text);
    }

    [Fact]
    public void FormatForNotification_Error_ContainsMessage()
    {
        var text = BalanceService.FormatForNotification(BalanceResult.Fail("HTTP 401"));
        Assert.Contains("HTTP 401", text);
    }
}
