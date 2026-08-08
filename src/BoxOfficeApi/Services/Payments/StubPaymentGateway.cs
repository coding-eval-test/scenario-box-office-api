namespace BoxOfficeApi.Services.Payments;

/// <summary>Deterministic in-process stub. Always succeeds.</summary>
public class StubPaymentGateway : IPaymentGateway
{
    private int _sequence;

    public Task<PaymentResult> ChargeAsync(string buyerEmail, decimal amount, CancellationToken cancellationToken)
    {
        var reference = $"stub-{Interlocked.Increment(ref _sequence):D6}";
        return Task.FromResult(new PaymentResult(reference));
    }
}
