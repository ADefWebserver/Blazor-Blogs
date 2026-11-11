using System.Globalization;

namespace BlazorBlogs.Data;

public static class StripeAmount
{
    private static readonly HashSet<string> ZeroDecimalCurrencies = new(
        ["bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ThreeDecimalCurrencies = new(
        ["bhd", "jod", "kwd", "omr", "tnd"],
        StringComparer.OrdinalIgnoreCase);

    public static long ToMinorUnits(decimal amount, string currency)
    {
        currency = ValidateCurrency(currency);
        var value = ZeroDecimalCurrencies.Contains(currency) ? amount : amount * 100m;
        return checked(decimal.ToInt64(decimal.Round(value, 0, MidpointRounding.AwayFromZero)));
    }

    public static decimal FromMinorUnits(long amount, string currency)
    {
        currency = ValidateCurrency(currency);
        return ZeroDecimalCurrencies.Contains(currency) ? amount : amount / 100m;
    }

    public static bool IsSupported(string currency)
    {
        currency = currency?.Trim().ToLowerInvariant();
        return currency is { Length: 3 }
            && !ThreeDecimalCurrencies.Contains(currency)
            && currency.All(char.IsLetter);
    }

    private static string ValidateCurrency(string currency)
    {
        currency = currency?.Trim().ToLowerInvariant();
        if (!IsSupported(currency))
            throw new ArgumentException("This three-decimal or invalid currency is not supported.", nameof(currency));
        return currency;
    }
}
