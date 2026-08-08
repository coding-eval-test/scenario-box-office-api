using BoxOfficeApi.Models;

namespace BoxOfficeApi.Data;

/// <summary>
/// Deterministic seed data. Every value derives arithmetically from a row index
/// or from <see cref="SeedBaseUtc"/> so the database is byte-identical on every
/// run. Never introduce DateTime.Now, Guid.NewGuid, or unseeded Random here.
/// </summary>
public static class DbSeeder
{
    public static readonly DateTime SeedBaseUtc = new(2026, 2, 15, 9, 0, 0, DateTimeKind.Utc);

    private static readonly string[] SeatNumbers = ["A", "B", "C", "D", "E"];

    /// <summary>Seats sold before the story starts, so seat maps look alive.</summary>
    private static readonly int[] BookedSeatIds = [1, 2, 8, 26, 32];

    public static void Seed(AppDbContext db)
    {
        if (db.Events.Any())
        {
            return;
        }

        db.Events.AddRange(
            new Event { Id = 1, Name = "Aurora in Concert", Venue = "Beacon Hall", OnSaleAtUtc = new DateTime(2026, 1, 15, 17, 0, 0, DateTimeKind.Utc) },
            new Event { Id = 2, Name = "The Glass Menagerie", Venue = "Harbor Playhouse", OnSaleAtUtc = new DateTime(2026, 2, 1, 17, 0, 0, DateTimeKind.Utc) },
            new Event { Id = 3, Name = "Winter Gala", Venue = "Beacon Hall", OnSaleAtUtc = new DateTime(2027, 6, 1, 17, 0, 0, DateTimeKind.Utc) });
        db.SaveChanges();

        db.Seats.AddRange(BuildSeats(eventId: 1, rows: 5, firstId: 1));
        db.Seats.AddRange(BuildSeats(eventId: 2, rows: 5, firstId: 26));
        db.Seats.AddRange(BuildSeats(eventId: 3, rows: 2, firstId: 51));
        db.SaveChanges();

        var bookingId = 0;
        foreach (var seatId in BookedSeatIds)
        {
            bookingId++;
            var seat = db.Seats.Single(s => s.Id == seatId);
            var ev = db.Events.Single(e => e.Id == seat.EventId);
            var buyerEmail = $"patron{bookingId}@example.test";
            var createdAtUtc = SeedBaseUtc.AddMinutes(bookingId * 10);

            db.Bookings.Add(new Booking
            {
                Id = bookingId,
                SeatId = seatId,
                BuyerEmail = buyerEmail,
                Amount = seat.Price,
                CreatedAtUtc = createdAtUtc
            });
            db.Payments.Add(new Payment
            {
                Id = bookingId,
                BookingId = bookingId,
                Amount = seat.Price,
                ProviderReference = $"seed-{bookingId:D6}",
                CreatedAtUtc = createdAtUtc
            });
            db.AuditEntries.Add(new AuditEntry
            {
                Id = bookingId,
                BookingId = bookingId,
                EventName = ev.Name,
                SeatLabel = $"{seat.Row}{seat.Number}",
                BuyerEmail = buyerEmail,
                Amount = seat.Price,
                CreatedAtUtc = createdAtUtc
            });
        }

        db.SaveChanges();
    }

    private static List<Seat> BuildSeats(int eventId, int rows, int firstId)
    {
        var seats = new List<Seat>(rows * SeatNumbers.Length);
        for (var row = 1; row <= rows; row++)
        {
            for (var numberIndex = 0; numberIndex < SeatNumbers.Length; numberIndex++)
            {
                var id = firstId + (row - 1) * SeatNumbers.Length + numberIndex;
                seats.Add(new Seat
                {
                    Id = id,
                    EventId = eventId,
                    Row = row,
                    Number = SeatNumbers[numberIndex],
                    Price = 40 + row * 5,
                    IsBooked = BookedSeatIds.Contains(id)
                });
            }
        }

        return seats;
    }
}
