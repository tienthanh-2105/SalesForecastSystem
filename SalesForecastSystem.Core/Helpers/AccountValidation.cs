namespace SalesForecastSystem.Core.Helpers;

public static class AccountValidation
{
    public const string EmailPattern = @"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$";
    public const string PasswordPattern = @"^(?=[\s\S]*[A-Za-z])(?=[\s\S]*[0-9])[\s\S]+$";
}
