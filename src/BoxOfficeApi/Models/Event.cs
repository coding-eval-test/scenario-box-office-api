namespace BoxOfficeApi.Models;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;

    /// <summary>The instant bookings open. Requests before this instant are rejected.</summary>
    public DateTime OnSaleAtUtc { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
