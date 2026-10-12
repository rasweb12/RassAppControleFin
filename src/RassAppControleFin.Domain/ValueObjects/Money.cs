namespace RassAppControleFin.Domain.ValueObjects;

public sealed record Money
{
    public const decimal Maximum = 99999999999999.99m;
    public decimal Amount { get; }
    public string Currency { get; }
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
    public static bool IsValid(decimal amount, string currency, bool allowZero = false)
        => (allowZero ? amount >= 0 : amount > 0) && amount <= Maximum
            && decimal.Round(amount, 2) == amount && currency is "BRL" or "USD" or "EUR";
    public static Money Create(decimal amount, string currency, bool allowZero = false)
    {
        if (!IsValid(amount, currency, allowZero)) throw new ArgumentException("Invalid money.");
        return new(amount, currency);
    }
}
