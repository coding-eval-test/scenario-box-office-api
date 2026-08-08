namespace BoxOfficeApi.Models;

public class Booking
{
    public int Id { get; set; }
    public int SeatId { get; set; }
    public Seat? Seat { get; set; }

    public string BuyerEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
