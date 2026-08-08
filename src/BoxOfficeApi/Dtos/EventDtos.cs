namespace BoxOfficeApi.Dtos;

public record EventResponse(
    int Id,
    string Name,
    string Venue,
    DateTime OnSaleAtUtc,
    int SeatCount,
    int AvailableSeatCount);

/// <param name="Status">Derived at read time: "Available" or "Booked".</param>
public record SeatResponse(
    int Id,
    int Row,
    string Number,
    string Label,
    decimal Price,
    string Status);

public record SeatMapResponse(
    int EventId,
    string EventName,
    IReadOnlyList<SeatResponse> Seats);
