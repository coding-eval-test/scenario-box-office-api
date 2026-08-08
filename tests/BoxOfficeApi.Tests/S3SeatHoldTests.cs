using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BoxOfficeApi.Tests;

/// <summary>
/// Covers the story described in docs/user-stories.md as S3. Seats 36-42 on
/// event 2. Expiry is evaluated on read against the injected clock — no
/// background job, no Task.Delay, no tolerance windows.
/// </summary>
public class S3SeatHoldTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public S3SeatHoldTests(ApiFactory factory) => _factory = factory;

    /// <summary>The response shape the hold endpoint is expected to return.</summary>
    private record HoldShape(int SeatId, string HolderKey, DateTime ExpiresAtUtc);

    private static Task<HttpResponseMessage> HoldAsync(HttpClient client, int seatId, string holderKey) =>
        client.PostAsJsonAsync($"/api/events/2/seats/{seatId}/holds", new { holderKey });

    private static Task<HttpResponseMessage> BookAsync(HttpClient client, int seatId, string buyerEmail) =>
        client.PostAsJsonAsync("/api/bookings", new { seatId, buyerEmail });

    [Fact]
    [Trait("Category", "S3")]
    public async Task The_holder_can_book_the_seat_they_hold()
    {
        var client = _factory.CreateClient();

        var hold = await HoldAsync(client, 36, "amy@example.test");
        Assert.Equal(HttpStatusCode.Created, hold.StatusCode);

        var booking = await BookAsync(client, 36, "amy@example.test");
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task An_active_hold_blocks_booking_by_anyone_else()
    {
        var client = _factory.CreateClient();

        var hold = await HoldAsync(client, 37, "amy@example.test");
        Assert.Equal(HttpStatusCode.Created, hold.StatusCode);

        var rival = await BookAsync(client, 37, "ben@example.test");
        Assert.Equal(HttpStatusCode.Conflict, rival.StatusCode);
        Assert.Equal("application/problem+json", rival.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task An_expired_hold_frees_the_seat_to_be_held_and_booked_by_others()
    {
        var client = _factory.CreateClient();

        var hold = await HoldAsync(client, 38, "amy@example.test");
        Assert.Equal(HttpStatusCode.Created, hold.StatusCode);

        // Default window is 5 minutes (Holds:DurationMinutes).
        _factory.Clock.Advance(TimeSpan.FromMinutes(6));

        var rivalHold = await HoldAsync(client, 38, "ben@example.test");
        Assert.Equal(HttpStatusCode.Created, rivalHold.StatusCode);

        var rivalBooking = await BookAsync(client, 38, "ben@example.test");
        Assert.Equal(HttpStatusCode.Created, rivalBooking.StatusCode);
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task Holding_an_already_held_seat_is_a_conflict()
    {
        var client = _factory.CreateClient();

        var first = await HoldAsync(client, 39, "amy@example.test");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await HoldAsync(client, 39, "ben@example.test");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task Holding_an_already_booked_seat_is_a_conflict()
    {
        var client = _factory.CreateClient();

        var booking = await BookAsync(client, 40, "carol@example.test");
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);

        var hold = await HoldAsync(client, 40, "late@example.test");
        Assert.Equal(HttpStatusCode.Conflict, hold.StatusCode);
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task The_hold_window_comes_from_configuration()
    {
        // A host with a 2-minute window over the same database and clock.
        using var configured = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Holds:DurationMinutes"] = "2"
                })));
        var client = configured.CreateClient();

        var hold = await HoldAsync(client, 41, "amy@example.test");
        Assert.Equal(HttpStatusCode.Created, hold.StatusCode);

        // Three minutes is inside the default 5-minute window but past the
        // configured 2-minute one — only a bound configuration passes this.
        _factory.Clock.Advance(TimeSpan.FromMinutes(3));

        var rival = await BookAsync(client, 41, "ben@example.test");
        Assert.Equal(HttpStatusCode.Created, rival.StatusCode);
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task A_hold_reports_its_expiry_instant()
    {
        var client = _factory.CreateClient();
        var expected = _factory.Clock.GetUtcNow().UtcDateTime.AddMinutes(5);

        var response = await HoldAsync(client, 42, "amy@example.test");
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var hold = await response.Content.ReadFromJsonAsync<HoldShape>();
        Assert.NotNull(hold);
        Assert.Equal(42, hold!.SeatId);
        Assert.Equal("amy@example.test", hold.HolderKey);
        Assert.Equal(expected, hold.ExpiresAtUtc);
    }
}
