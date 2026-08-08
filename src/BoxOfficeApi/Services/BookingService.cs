using BoxOfficeApi.Data;
using BoxOfficeApi.Dtos;
using BoxOfficeApi.Models;
using BoxOfficeApi.Services.Exceptions;
using BoxOfficeApi.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace BoxOfficeApi.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly TimeProvider _clock;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        AppDbContext db,
        IPaymentGateway gateway,
        TimeProvider clock,
        ILogger<BookingService> logger)
    {
        _db = db;
        _gateway = gateway;
        _clock = clock;
        _logger = logger;
    }

    public async Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var seatId = request.SeatId!.Value;
        var seat = await _db.Seats.FindAsync(new object[] { seatId }, cancellationToken);
        if (seat is null)
        {
            throw new NotFoundException($"Seat {seatId} was not found.");
        }

        var ev = await _db.Events.FindAsync(new object[] { seat.EventId }, cancellationToken);

        if (_clock.GetLocalNow().DateTime < ev!.OnSaleAtUtc)
        {
            throw new ConflictException($"Tickets for {ev.Name} are not on sale yet.");
        }

        if (seat.IsBooked)
        {
            throw new ConflictException($"Seat {seat.Row}{seat.Number} is already booked.");
        }

        var nowUtc = _clock.GetUtcNow().UtcDateTime;

        seat.IsBooked = true;
        var booking = new Booking
        {
            SeatId = seat.Id,
            BuyerEmail = request.BuyerEmail,
            Amount = seat.Price,
            CreatedAtUtc = nowUtc
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        var charge = await _gateway.ChargeAsync(request.BuyerEmail, seat.Price, cancellationToken);
        _db.Payments.Add(new Payment
        {
            BookingId = booking.Id,
            Amount = seat.Price,
            ProviderReference = charge.ProviderReference,
            CreatedAtUtc = nowUtc
        });
        await _db.SaveChangesAsync(cancellationToken);

        _db.AuditEntries.Add(new AuditEntry
        {
            BookingId = booking.Id,
            EventName = ev.Name,
            SeatLabel = $"{seat.Row}{seat.Number}",
            BuyerEmail = request.BuyerEmail,
            Amount = seat.Price,
            CreatedAtUtc = nowUtc
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Booked seat {SeatId} for {Buyer}", seat.Id, request.BuyerEmail);

        return Map(booking, seat, ev);
    }

    public async Task<BookingResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var booking = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Seat!)
            .ThenInclude(s => s.Event)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (booking is null)
        {
            throw new NotFoundException($"Booking {id} was not found.");
        }

        return Map(booking, booking.Seat!, booking.Seat!.Event!);
    }

    private static BookingResponse Map(Booking booking, Seat seat, Event ev) => new(
        booking.Id,
        seat.Id,
        ev.Id,
        $"{seat.Row}{seat.Number}",
        booking.BuyerEmail,
        booking.Amount,
        booking.CreatedAtUtc);
}
