using Talk.Core;

namespace Talk.Core.Tests;

public class ConsoleHelpersTests
{
    [Theory]
    [InlineData("exit")]
    [InlineData("EXIT")]
    [InlineData("quit")]
    [InlineData("q")]
    [InlineData("Q")]
    [InlineData(null)]
    public void IsExit_WithExitSignal_ReturnsTrue(string? input)
    {
        Assert.True(ConsoleHelpers.IsExit(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("exit now")]
    public void IsExit_WithNonExitInput_ReturnsFalse(string input)
    {
        Assert.False(ConsoleHelpers.IsExit(input));
    }
}
