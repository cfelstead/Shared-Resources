namespace Talk.Core;

public static class ConsoleHelpers
{
    public static bool IsExit(string? input)
    {
        if (input is null)
        {
            // Console.ReadLine() returns null on EOF (e.g. piped/redirected stdin
            // runs out) - treat that as an exit signal rather than looping forever.
            return true;
        }

        return string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(input, "quit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(input, "q", StringComparison.OrdinalIgnoreCase);
    }

    public static void WriteHeader(string title, string concept)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine(title);
        Console.WriteLine(concept);
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("Type 'exit' to quit.");
        Console.WriteLine();
    }
}
