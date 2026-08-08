namespace BoxOfficeApi.Services.Exceptions;

/// <summary>Thrown when the payment provider declines a charge. Surfaces as HTTP 402.</summary>
public class PaymentFailedException : Exception
{
    public PaymentFailedException(string message) : base(message)
    {
    }
}
