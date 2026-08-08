using System.Net;
using System.Net.Http.Json;
using BoxOfficeApi.Dtos;
using Xunit;

namespace BoxOfficeApi.Tests;

public class BookingsRegressionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public BookingsRegressionTests(ApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Happy_path_booking_returns_201_with_the_seat_price()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId = 11,
            buyerEmail = "buyer11@example.test"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(booking);
        Assert.Equal(11, booking!.SeatId);
        Assert.Equal(1, booking.EventId);
        Assert.Equal("3A", booking.SeatLabel);
        Assert.Equal(55m, booking.Amount);
        Assert.Equal("buyer11@example.test", booking.BuyerEmail);

        // The Location header must resolve to the same booking.
        Assert.NotNull(response.Headers.Location);
        var fetched = await client.GetFromJsonAsync<BookingResponse>(response.Headers.Location);
        Assert.Equal(booking.Id, fetched!.Id);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Booking_a_seat_that_is_already_booked_is_a_409()
    {
        var client = _factory.CreateClient();

        // Seat 1 ships booked in the seed.
        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId = 1,
            buyerEmail = "late@example.test"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Booking_an_unknown_seat_is_a_404()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId = 9999,
            buyerEmail = "nowhere@example.test"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Booking_before_the_on_sale_instant_is_rejected()
    {
        var client = _factory.CreateClient();

        // Seat 55 belongs to Winter Gala, which goes on sale 2027-06-01T17:00Z.
        // The suite's clock reads 2026-03-01T12:00Z in a UTC local zone.
        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId = 55,
            buyerEmail = "early@example.test"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task A_missing_buyer_email_is_rejected_before_the_database()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/bookings", new { seatId = 12 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task An_invalid_buyer_email_is_rejected_before_the_database()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId = 12,
            buyerEmail = "not-an-email"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task A_booking_shows_up_on_the_seat_map()
    {
        var client = _factory.CreateClient();

        var created = await client.PostAsJsonAsync("/api/bookings", new
        {
            seatId = 13,
            buyerEmail = "buyer13@example.test"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var map = await client.GetFromJsonAsync<SeatMapResponse>("/api/events/1/seats");
        Assert.Equal("Booked", map!.Seats.Single(s => s.Id == 13).Status);
    }
}
