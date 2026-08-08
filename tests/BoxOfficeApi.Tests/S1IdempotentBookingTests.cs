using System.Net;
using System.Net.Http.Json;
using BoxOfficeApi.Dtos;
using Xunit;

namespace BoxOfficeApi.Tests;

/// <summary>
/// Covers the story described in docs/user-stories.md as S1. Seats 16-20.
/// </summary>
public class S1IdempotentBookingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public S1IdempotentBookingTests(ApiFactory factory) => _factory = factory;

    private static async Task<HttpResponseMessage> PostBookingAsync(
        HttpClient client, int seatId, string buyerEmail, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bookings")
        {
            Content = JsonContent.Create(new { seatId, buyerEmail })
        };
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await client.SendAsync(request);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task Replaying_the_same_key_returns_the_original_booking()
    {
        var client = _factory.CreateClient();

        var first = await PostBookingAsync(client, 16, "amy@example.test", "key-seat-16");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var original = await first.Content.ReadFromJsonAsync<BookingResponse>();

        var replay = await PostBookingAsync(client, 16, "amy@example.test", "key-seat-16");
        Assert.True(replay.IsSuccessStatusCode,
            $"Replaying the same key must succeed, got {(int)replay.StatusCode}.");
        var replayed = await replay.Content.ReadFromJsonAsync<BookingResponse>();

        Assert.Equal(original!.Id, replayed!.Id);
        Assert.Equal(original.SeatId, replayed.SeatId);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task A_replayed_key_creates_no_second_booking_payment_or_charge()
    {
        var client = _factory.CreateClient();
        var chargesBefore = _factory.Gateway.ChargeCount;

        var first = await PostBookingAsync(client, 17, "ben@example.test", "key-seat-17");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var replay = await PostBookingAsync(client, 17, "ben@example.test", "key-seat-17");
        Assert.True(replay.IsSuccessStatusCode,
            $"Replaying the same key must succeed, got {(int)replay.StatusCode}.");

        using var db = _factory.CreateDbContext();
        var bookingIds = db.Bookings.Where(b => b.SeatId == 17).Select(b => b.Id).ToList();
        Assert.Single(bookingIds);
        Assert.Equal(1, db.Payments.Count(p => bookingIds.Contains(p.BookingId)));
        Assert.Equal(1, _factory.Gateway.ChargeCount - chargesBefore);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task A_replay_succeeds_even_though_a_fresh_request_for_the_seat_would_conflict()
    {
        var client = _factory.CreateClient();

        var first = await PostBookingAsync(client, 18, "cho@example.test", "key-seat-18");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var original = await first.Content.ReadFromJsonAsync<BookingResponse>();

        // The seat is now unavailable: a keyless request must conflict...
        var fresh = await PostBookingAsync(client, 18, "someone.else@example.test");
        Assert.Equal(HttpStatusCode.Conflict, fresh.StatusCode);

        // ...but the replay must still return the original success.
        var replay = await PostBookingAsync(client, 18, "cho@example.test", "key-seat-18");
        Assert.True(replay.IsSuccessStatusCode,
            $"Replay after the seat became unavailable must succeed, got {(int)replay.StatusCode}.");
        var replayed = await replay.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal(original!.Id, replayed!.Id);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task Different_keys_behave_as_independent_requests()
    {
        var client = _factory.CreateClient();

        var first = await PostBookingAsync(client, 19, "dee@example.test", "key-seat-19");
        var second = await PostBookingAsync(client, 20, "eli@example.test", "key-seat-20");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var a = await first.Content.ReadFromJsonAsync<BookingResponse>();
        var b = await second.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotEqual(a!.Id, b!.Id);

        using var db = _factory.CreateDbContext();
        Assert.Equal(1, db.Bookings.Count(x => x.SeatId == 19));
        Assert.Equal(1, db.Bookings.Count(x => x.SeatId == 20));
    }
}
