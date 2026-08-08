namespace BoxOfficeApi.Models;

public class Seat
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }

    /// <summary>Row and number combine into the label buyers see, e.g. row 12 + "A" = "12A".</summary>
    public int Row { get; set; }
    public string Number { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsBooked { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
