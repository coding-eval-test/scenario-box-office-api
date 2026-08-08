using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BoxOfficeApi.Tests;

/// <summary>
/// Covers the defect reported in docs/bug-reports.md as B2. Seats 51-54 on
/// Winter Gala, which goes on sale at 2027-06-01T17:00:00Z. Each test pins the
/// fake clock to an instant just either side of that boundary, in a local zone
/// ahead of or behind UTC.
/// </summary>
public class B2OnSaleGateTests
{
    private static readonly DateTimeOffset JustBeforeOnSale = new(2027, 6, 1, 16, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset JustAfterOnSale = new(2027, 6, 1, 17, 30, 0, TimeSpan.Zero);

    private static readonly TimeZoneInfo AheadOfUtc = TimeZoneInfo.CreateCustomTimeZone(
        "Test UTC+09", TimeSpan.FromHours(9), "Test UTC+09", "Test UTC+09");

    private static readonly TimeZoneInfo BehindUtc = TimeZoneInfo.CreateCustomTimeZone(
        "Test UTC-06", TimeSpan.FromHours(-6), "Test UTC-06", "Test UTC-06");

    private static async Task<HttpResponseMessage> BookAtAsync(
        ApiFactory factory, TimeZoneInfo zone, DateTimeOffset utcNow, int seatId)
    {
        factory.Clock.SetLocalTimeZone(zone);
        factory.Clock.SetUtcNow(utcNow);
        var client = factory.CreateClient();
        return await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId,
            buyerEmail = "buyer@example.test"
        });
    }

    [Fact]
    [Trait("Category", "B2")]
    public async Task Before_on_sale_is_rejected_even_where_local_clocks_run_ahead_of_utc()
    {
        using var factory = new ApiFactory();

        var response = await BookAtAsync(factory, AheadOfUtc, JustBeforeOnSale, seatId: 51);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "B2")]
    public async Task After_on_sale_is_accepted_even_where_local_clocks_run_behind_utc()
    {
        using var factory = new ApiFactory();

        var response = await BookAtAsync(factory, BehindUtc, JustAfterOnSale, seatId: 52);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "B2")]
    public async Task After_on_sale_is_accepted_where_local_clocks_run_ahead_of_utc()
    {
        using var factory = new ApiFactory();

        var response = await BookAtAsync(factory, AheadOfUtc, JustAfterOnSale, seatId: 53);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "B2")]
    public async Task Before_on_sale_is_rejected_where_local_clocks_run_behind_utc()
    {
        using var factory = new ApiFactory();

        var response = await BookAtAsync(factory, BehindUtc, JustBeforeOnSale, seatId: 54);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
