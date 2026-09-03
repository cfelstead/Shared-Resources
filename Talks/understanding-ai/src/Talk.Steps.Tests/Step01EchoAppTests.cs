using Step01Echo;

namespace Talk.Steps.Tests;

public class Step01EchoAppTests
{
    [Fact]
    public void Run_EchoesInputUntilExit()
    {
        var input = new StringReader("hello\nexit\n");
        var output = new StringWriter();

        Step01EchoApp.Run(input, output);

        Assert.Contains("Echo: hello", output.ToString());
    }
}
