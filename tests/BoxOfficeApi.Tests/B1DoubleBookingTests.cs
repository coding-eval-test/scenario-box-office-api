using System.Net;
using System.Net.Http.Json;
using BoxOfficeApi.Data;
using BoxOfficeApi.Dtos;
using BoxOfficeApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoxOfficeApi.Tests;

/// <summary>
/// Covers the defect reported in docs/bug-reports.md as B1. Seats 46-48.
/// Contention is modeled by two explicitly interleaved DbContext scopes that
/// both read the seat while it is free — no threads, no races, same outcome
/// every run.
/// </summary>
public class B1DoubleBookingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public B1DoubleBookingTests(ApiFactory factory) => _factory = factory;

    private async Task<IServiceScope> ScopeWithSeatLoadedAsync(int seatId)
    {
        // Loading the seat through the scope's DbContext models a request that
        // has read the seat map while the seat was still free.
        var scope = _factory.Services.CreateScope();
        _ = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Seats.SingleAsync(s => s.Id == seatId);
        return scope;
    }

    private static Task<BookingResponse> BookThroughAsync(IServiceScope scope, int seatId, string buyerEmail) =>
        scope.ServiceProvider.GetRequiredService<IBookingService>().CreateAsync(
            new CreateBookingRequest { SeatId = seatId, BuyerEmail = buyerEmail },
            CancellationToken.None);

    [Fact]
    [Trait("Category", "B1")]
    public async Task Interleaved_bookings_leave_one_booking_and_one_payment_and_reject_the_loser()
    {
        const int seatId = 46;
        using var winnerScope = await ScopeWithSeatLoadedAsync(seatId);
        using var loserScope = await ScopeWithSeatLoadedAsync(seatId);

        var won = await BookThroughAsync(winnerScope, seatId, "winner@example.test");
        Assert.Equal(seatId, won.SeatId);

        var thrown = await Record.ExceptionAsync(
            () => BookThroughAsync(loserScope, seatId, "loser@example.test"));

        Assert.True(thrown is not null,
            "The losing request silently succeeded - the seat was double-booked.");

        using var db = _factory.CreateDbContext();
        var bookingIds = db.Bookings.Where(b => b.SeatId == seatId).Select(b => b.Id).ToList();
        Assert.Single(bookingIds);
        Assert.Equal(1, db.Payments.Count(p => bookingIds.Contains(p.BookingId)));
    }

    [Fact]
    [Trait("Category", "B1")]
    public async Task An_interleaved_loser_against_an_http_winner_conflicts_and_surfaces_as_409()
    {
        const int seatId = 47;
        using var loserScope = await ScopeWithSeatLoadedAsync(seatId);

        var client = _factory.CreateClient();
        var winner = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId,
            buyerEmail = "winner@example.test"
        });
        Assert.Equal(HttpStatusCode.Created, winner.StatusCode);

        var thrown = await Record.ExceptionAsync(
            () => BookThroughAsync(loserScope, seatId, "loser@example.test"));
        Assert.True(thrown is not null,
            "The losing request silently succeeded - the seat was double-booked.");

        // The losing path's public contract: a conflict, as problem+json.
        var losingRequest = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId,
            buyerEmail = "third@example.test"
        });
        Assert.Equal(HttpStatusCode.Conflict, losingRequest.StatusCode);
        Assert.Equal("application/problem+json", losingRequest.Content.Headers.ContentType?.MediaType);

        using var db = _factory.CreateDbContext();
        Assert.Equal(1, db.Bookings.Count(b => b.SeatId == seatId));
    }

    [Fact]
    [Trait("Category", "B1")]
    public async Task Interleaved_bookings_write_exactly_one_audit_row_for_the_seat()
    {
        const int seatId = 48;
        using var winnerScope = await ScopeWithSeatLoadedAsync(seatId);
        using var loserScope = await ScopeWithSeatLoadedAsync(seatId);

        await BookThroughAsync(winnerScope, seatId, "winner@example.test");
        _ = await Record.ExceptionAsync(
            () => BookThroughAsync(loserScope, seatId, "loser@example.test"));

        using var db = _factory.CreateDbContext();
        var bookingIds = db.Bookings.Where(b => b.SeatId == seatId).Select(b => b.Id).ToList();
        Assert.Single(bookingIds);
        Assert.Equal(1, db.AuditEntries.Count(a => bookingIds.Contains(a.BookingId)));
    }
}
