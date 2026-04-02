namespace PayFlow.Domain.Exceptions;

public class CurrencyMismatchException : Exception
{
    public CurrencyMismatchException()
        : base("Source and destination wallets must use the same currency.")
    {
    }

    public CurrencyMismatchException(string sourceCurrency, string destCurrency)
        : base($"Currency mismatch: source wallet uses {sourceCurrency}, destination uses {destCurrency}.")
    {
    }
}
