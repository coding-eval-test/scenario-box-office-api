namespace BoxOfficeApi.Models;

/// <summary>One row per confirmed booking, written for the venue's reconciliation.</summary>
public class AuditEntry
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public string EventName { get; set; } = string.Empty;
    public string SeatLabel { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
