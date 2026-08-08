namespace BoxOfficeApi.Services.Payments;

/// <summary>Result of a successful charge.</summary>
public record PaymentResult(string ProviderReference);

/// <summary>
/// The payment provider seam. The shipped implementation is an in-process stub;
/// there is no network call anywhere behind this interface.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(string buyerEmail, decimal amount, CancellationToken cancellationToken);
}
