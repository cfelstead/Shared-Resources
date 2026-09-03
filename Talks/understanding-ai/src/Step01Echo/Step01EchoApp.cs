using Talk.Core;

namespace Step01Echo;

public static class Step01EchoApp
{
    public static void Run(TextReader input, TextWriter output)
    {
        while (true)
        {
            output.Write("You: ");
            var line = input.ReadLine();
            if (ConsoleHelpers.IsExit(line))
            {
                break;
            }

            output.WriteLine($"Echo: {line}");
        }
    }
}
