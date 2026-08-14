using Xunit;

namespace DshLauncher.Tests;

public class VersionTests
{
    [Theory]
    [InlineData("0.3.4", "0.3.4", 0)]
    [InlineData("0.3.5", "0.3.4", 1)]
    [InlineData("0.3.3", "0.3.4", -1)]
    [InlineData("1.0.0", "0.3.4", 1)]
    [InlineData("0.10.0", "0.9.0", 1)]
    public void CompareVersions_ReturnsCorrectResult(string v1, string v2, int expected)
    {
        // Access the private method via reflection for testing
        var method = typeof(UpdateChecker).GetMethod("CompareVersions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (int)method!.Invoke(null, new object[] { v1, v2 })!;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CurrentVersion_IsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(UpdateChecker.CurrentVersion));
    }

    [Fact]
    public void CurrentVersion_IsSemanticVersion()
    {
        var parts = UpdateChecker.CurrentVersion.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.All(parts, p => Assert.True(int.TryParse(p, out _)));
    }
}
