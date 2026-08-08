using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BoxOfficeApi.Tests;

/// <summary>
/// Covers the story described in docs/user-stories.md as S2. Seats 21-25.
/// Row counts are compared as before/after totals so the seeded rows and any
/// earlier test in this class cannot skew the numbers.
/// </summary>
public class S2AtomicConfirmationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public S2AtomicConfirmationTests(ApiFactory factory) => _factory = factory;

    private (int Bookings, int Payments, int Audits) Totals()
    {
        using var db = _factory.CreateDbContext();
        return (db.Bookings.Count(), db.Payments.Count(), db.AuditEntries.Count());
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task A_declined_charge_leaves_no_booking_payment_or_audit_row()
    {
        var client = _factory.CreateClient();
        var before = Totals();

        _factory.Gateway.FailAlways = true;
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/api/bookings", new
            {
                seatId = 21,
                buyerEmail = "fran@example.test"
            });
        }
        finally
        {
            _factory.Gateway.FailAlways = false;
        }

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(before, Totals());
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task A_declined_charge_leaves_the_seat_available_for_the_next_buyer()
    {
        var client = _factory.CreateClient();

        _factory.Gateway.FailAlways = true;
        try
        {
            var declined = await client.PostAsJsonAsync("/api/bookings", new
            {
                seatId = 22,
                buyerEmail = "gus@example.test"
            });
            Assert.Equal(HttpStatusCode.PaymentRequired, declined.StatusCode);
        }
        finally
        {
            _factory.Gateway.FailAlways = false;
        }

        var retry = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId = 22,
            buyerEmail = "hana@example.test"
        });

        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task A_successful_booking_writes_exactly_one_of_each_row()
    {
        var client = _factory.CreateClient();
        var before = Totals();

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId = 23,
            buyerEmail = "iris@example.test"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var after = Totals();
        Assert.Equal(before.Bookings + 1, after.Bookings);
        Assert.Equal(before.Payments + 1, after.Payments);
        Assert.Equal(before.Audits + 1, after.Audits);

        using var db = _factory.CreateDbContext();
        var booking = db.Bookings.Single(b => b.SeatId == 23);
        Assert.Equal(1, db.Payments.Count(p => p.BookingId == booking.Id));
        Assert.Equal(1, db.AuditEntries.Count(a => a.BookingId == booking.Id));
    }
}
