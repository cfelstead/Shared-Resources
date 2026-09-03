namespace Step04NativeSkills;

public sealed class UnitConversionSkill
{
    public double ConvertFahrenheitToCelsius(double fahrenheit)
    {
        return Math.Round((fahrenheit - 32d) * 5d / 9d, 2);
    }

    public double ConvertCelsiusToFahrenheit(double celsius)
    {
        return Math.Round((celsius * 9d / 5d) + 32d, 2);
    }

    public double ConvertFeetToCentimeters(double feet)
    {
        return Math.Round(feet * 30.48d, 2);
    }
}
