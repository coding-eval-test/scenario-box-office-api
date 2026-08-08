using System.ComponentModel.DataAnnotations;

namespace BoxOfficeApi.Dtos;

public record BookingResponse(
    int Id,
    int SeatId,
    int EventId,
    string SeatLabel,
    string BuyerEmail,
    decimal Amount,
    DateTime CreatedAtUtc);

public class CreateBookingRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int? SeatId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string BuyerEmail { get; set; } = string.Empty;
}
