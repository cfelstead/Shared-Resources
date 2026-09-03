using Microsoft.Extensions.AI;
using Talk.Core;

namespace Step04NativeSkills;

public static class Step04NativeSkillsApp
{
    public static async Task RunAsync(IChatClient chatClient, TextReader input, TextWriter output)
    {
        var skill = new UnitConversionSkill();

        var fahrenheitToCelsiusTool = AIFunctionFactory.Create(
            skill.ConvertFahrenheitToCelsius,
            new AIFunctionFactoryOptions
            {
                Name = "convert_fahrenheit_to_celsius",
                Description = "Converts Fahrenheit temperature to Celsius with deterministic math."
            });

        var celsiusToFahrenheitTool = AIFunctionFactory.Create(
            skill.ConvertCelsiusToFahrenheit,
            new AIFunctionFactoryOptions
            {
                Name = "convert_celsius_to_fahrenheit",
                Description = "Converts Celsius temperature to Fahrenheit with deterministic math."
            });

        while (true)
        {
            output.Write("You: ");
            var line = input.ReadLine();
            if (ConsoleHelpers.IsExit(line))
            {
                break;
            }

            var options = new ChatOptions
            {
                Tools = new List<AITool>
                {
                    fahrenheitToCelsiusTool,
                    celsiusToFahrenheitTool
                }
            };

            var response = await chatClient.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, line ?? string.Empty) },
                options);

            output.WriteLine($"LLM: {response.Text}");
        }
    }
}
