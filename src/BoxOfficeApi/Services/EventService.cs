using BoxOfficeApi.Data;
using BoxOfficeApi.Dtos;
using BoxOfficeApi.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BoxOfficeApi.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _db;

    public EventService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EventResponse>> ListAsync(CancellationToken cancellationToken) =>
        await _db.Events
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .Select(e => new EventResponse(
                e.Id,
                e.Name,
                e.Venue,
                e.OnSaleAtUtc,
                e.Seats.Count,
                e.Seats.Count(s => !s.IsBooked)))
            .ToListAsync(cancellationToken);

    public async Task<SeatMapResponse> GetSeatMapAsync(int eventId, CancellationToken cancellationToken)
    {
        var ev = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (ev is null)
        {
            throw new NotFoundException($"Event {eventId} was not found.");
        }

        // The label is composed in memory: SQLite cannot concatenate an integer
        // and a string without a cast EF does not generate.
        var seats = (await _db.Seats
                .AsNoTracking()
                .Where(s => s.EventId == eventId)
                .OrderBy(s => s.Row).ThenBy(s => s.Number)
                .Select(s => new { s.Id, s.Row, s.Number, s.Price, s.IsBooked })
                .ToListAsync(cancellationToken))
            .Select(s => new SeatResponse(
                s.Id, s.Row, s.Number, $"{s.Row}{s.Number}", s.Price,
                s.IsBooked ? "Booked" : "Available"))
            .ToList();

        return new SeatMapResponse(ev.Id, ev.Name, seats);
    }
}
