using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DshLauncher;

/// <summary>One balance entry returned by the DeepSeek balance API.</summary>
public sealed record BalanceEntry(string Currency, decimal Total, decimal Granted, decimal ToppedUp);

/// <summary>Result of a balance query.</summary>
public sealed record BalanceResult(bool Ok, IReadOnlyList<BalanceEntry> Balances, string? Error)
{
    public static BalanceResult Fail(string error) => new(false, Array.Empty<BalanceEntry>(), error);
}

/// <summary>
/// DeepSeek account balance query.
/// Key sources: DEEPSEEK_API_KEY env var, then DSH_HOME/.credentials.yaml.
/// Endpoint: https://api.deepseek.com/user/balance, overridable via
/// DEEPSEEK_BALANCE_URL (full URL) or DEEPSEEK_API_BASE (base URL).
/// </summary>
public static class BalanceService
{
    private const string DefaultBase = "https://api.deepseek.com";

    /// <summary>Read the API key from env or the credentials file; empty when unavailable.</summary>
    public static string ReadApiKey(string dshHome)
    {
        var envKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey)) return envKey.Trim();
        try
        {
            var text = File.ReadAllText(Path.Combine(dshHome, ".credentials.yaml"));
            foreach (var line in text.Split('\n'))
            {
                var m = System.Text.RegularExpressions.Regex.Match(line, @"^\s*DEEPSEEK_API_KEY\s*:\s*[""']?([^""'\s#]+)");
                if (m.Success) return m.Groups[1].Value;
            }
        }
        catch { /* no credentials file */ }
        return "";
    }

    /// <summary>Resolve the balance endpoint with env overrides.</summary>
    public static string BalanceEndpoint()
    {
        var envUrl = Environment.GetEnvironmentVariable("DEEPSEEK_BALANCE_URL");
        if (!string.IsNullOrWhiteSpace(envUrl)) return envUrl;
        var baseUrl = (Environment.GetEnvironmentVariable("DEEPSEEK_API_BASE") ?? DefaultBase).TrimEnd('/');
        return baseUrl + "/user/balance";
    }

    /// <summary>Parse the balance JSON payload (public for tests).</summary>
    public static BalanceResult ParseBalance(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return BalanceResult.Fail("JSON 格式异常");
            var infos = root.TryGetProperty("balance_infos", out var arr) && arr.ValueKind == JsonValueKind.Array
                ? arr
                : default;
            var balances = new List<BalanceEntry>();
            if (infos.ValueKind == JsonValueKind.Array)
            {
                foreach (var b in infos.EnumerateArray())
                {
                    balances.Add(new BalanceEntry(
                        b.TryGetProperty("currency", out var c) ? c.GetString() ?? "" : "",
                        GetDecimal(b, "total_balance"),
                        GetDecimal(b, "granted_balance"),
                        GetDecimal(b, "topped_up_balance")));
                }
            }
            return new BalanceResult(true, balances, null);
        }
        catch (Exception err)
        {
            return BalanceResult.Fail(err.Message);
        }
    }

    private static decimal GetDecimal(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var v)) return 0m;
        // the API returns balances as JSON strings ("100.50")
        if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var s)) return s;
        return v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : 0m;
    }

    /// <summary>Query the balance API (15s timeout).</summary>
    public static async Task<BalanceResult> QueryBalanceAsync(string dshHome)
    {
        var key = ReadApiKey(dshHome);
        if (key.Length == 0) return BalanceResult.Fail("no-key");
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            using var req = new HttpRequestMessage(HttpMethod.Get, BalanceEndpoint());
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            req.Headers.UserAgent.ParseAdd("DSH-Launcher");
            using var res = await client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                return BalanceResult.Fail($"HTTP {(int)res.StatusCode}: {body[..Math.Min(200, body.Length)]}");
            return ParseBalance(body);
        }
        catch (Exception err)
        {
            return BalanceResult.Fail(err.Message);
        }
    }

    /// <summary>Format a balance result as a short single-line text for notifications.</summary>
    public static string FormatForNotification(BalanceResult result)
    {
        if (!result.Ok) return result.Error == "no-key"
            ? "未找到 DEEPSEEK_API_KEY（设置环境变量或 ~/.dsh/.credentials.yaml）"
            : $"余额查询失败：{result.Error}";
        if (result.Balances.Count == 0) return "余额查询成功，但响应中没有余额数据";
        var b = result.Balances[0];
        var parts = new List<string> { $"{b.Currency} {b.Total:0.##}" };
        if (b.Granted > 0) parts.Add($"含赠金 {b.Granted:0.##}");
        if (b.ToppedUp > 0) parts.Add($"充值 {b.ToppedUp:0.##}");
        return "DeepSeek 余额: " + string.Join(", ", parts);
    }
}
