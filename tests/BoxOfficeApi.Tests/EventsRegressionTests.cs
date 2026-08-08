using System.Net;
using System.Net.Http.Json;
using BoxOfficeApi.Dtos;
using Xunit;

namespace BoxOfficeApi.Tests;

public class EventsRegressionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventsRegressionTests(ApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Event_list_returns_the_three_seeded_events_with_counts()
    {
        var client = _factory.CreateClient();

        var events = await client.GetFromJsonAsync<List<EventResponse>>("/api/events");

        Assert.NotNull(events);
        Assert.Equal(3, events!.Count);
        Assert.Equal(
            new[] { "Aurora in Concert", "The Glass Menagerie", "Winter Gala" },
            events.Select(e => e.Name).ToArray());
        Assert.Equal(new DateTime(2027, 6, 1, 17, 0, 0, DateTimeKind.Utc), events[2].OnSaleAtUtc);
        Assert.Equal(25, events[0].SeatCount);
        Assert.Equal(22, events[0].AvailableSeatCount);
        Assert.Equal(10, events[2].SeatCount);
        Assert.Equal(10, events[2].AvailableSeatCount);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Seat_map_derives_availability_from_the_seeded_bookings()
    {
        var client = _factory.CreateClient();

        var map = await client.GetFromJsonAsync<SeatMapResponse>("/api/events/1/seats");

        Assert.NotNull(map);
        Assert.Equal("Aurora in Concert", map!.EventName);
        Assert.Equal(25, map.Seats.Count);
        Assert.Equal("1A", map.Seats[0].Label);
        Assert.Equal("5E", map.Seats[^1].Label);
        Assert.Equal(45m, map.Seats[0].Price);
        Assert.Equal(
            new[] { 1, 2, 8 },
            map.Seats.Where(s => s.Status == "Booked").Select(s => s.Id).ToArray());
        Assert.Equal(22, map.Seats.Count(s => s.Status == "Available"));
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Seat_map_for_an_unknown_event_is_a_problem_details_404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/9999/seats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
