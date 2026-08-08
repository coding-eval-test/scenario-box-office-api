namespace BoxOfficeApi.Models;

/// <summary>Record of the charge behind a booking. One per booking.</summary>
public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public decimal Amount { get; set; }
    public string ProviderReference { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
