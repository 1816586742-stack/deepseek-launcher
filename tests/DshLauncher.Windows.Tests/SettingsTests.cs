using Xunit;

namespace DshLauncher.Tests;

public class SettingsTests
{
    [Fact]
    public void Port_DefaultValue_Is3080()
    {
        Assert.Equal(3080, SettingsManager.Port);
    }

    [Fact]
    public void AutoStartDsh_DefaultValue_IsTrue()
    {
        Assert.True(SettingsManager.AutoStartDsh);
    }

    [Fact]
    public void LoadSettings_DoesNotThrow()
    {
        // Should not throw even with missing/corrupt config
        var exception = Record.Exception(() => SettingsManager.Load());
        Assert.Null(exception);
    }
}
