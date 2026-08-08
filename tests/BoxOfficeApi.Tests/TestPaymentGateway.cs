using BoxOfficeApi.Services.Payments;
using BoxOfficeApi.Services.Exceptions;

namespace BoxOfficeApi.Tests;

/// <summary>
/// The gateway the whole suite charges against: counts every invocation (S1
/// asserts a replay charges once) and can be flipped to decline (S2 asserts a
/// failed charge leaves no rows). Deterministic, in-process, single-threaded —
/// the suite never runs requests concurrently.
/// </summary>
public class TestPaymentGateway : IPaymentGateway
{
    public int ChargeCount { get; private set; }

    public bool FailAlways { get; set; }

    public Task<PaymentResult> ChargeAsync(string buyerEmail, decimal amount, CancellationToken cancellationToken)
    {
        ChargeCount++;
        if (FailAlways)
        {
            throw new PaymentFailedException("Declined by the test gateway.");
        }

        return Task.FromResult(new PaymentResult($"test-{ChargeCount:D6}"));
    }
}
