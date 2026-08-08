using BoxOfficeApi.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BoxOfficeApi.Tests;

/// <summary>
/// The seed is the fixture every other test reasons about. If it drifts, every
/// expectation in the suite becomes unreliable, so it is pinned here.
/// </summary>
public class SeedDeterminismTests
{
    private static AppDbContext NewSeededContext(out string path)
    {
        path = Path.Combine(Path.GetTempPath(), $"seed-tests-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        var db = new AppDbContext(options);
        db.Database.Migrate();
        DbSeeder.Seed(db);
        return db;
    }

    private static void CleanUp(AppDbContext db, string path)
    {
        db.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(path);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Seed_produces_the_documented_row_counts()
    {
        var db = NewSeededContext(out var path);

        Assert.Equal(3, db.Events.Count());
        Assert.Equal(60, db.Seats.Count());
        Assert.Equal(5, db.Bookings.Count());
        Assert.Equal(5, db.Payments.Count());
        Assert.Equal(5, db.AuditEntries.Count());
        Assert.Equal(25, db.Seats.Count(s => s.EventId == 1));
        Assert.Equal(25, db.Seats.Count(s => s.EventId == 2));
        Assert.Equal(10, db.Seats.Count(s => s.EventId == 3));
        Assert.Equal(
            new[] { 1, 2, 8, 26, 32 },
            db.Seats.Where(s => s.IsBooked).OrderBy(s => s.Id).Select(s => s.Id).ToArray());

        CleanUp(db, path);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Seed_is_identical_across_two_independent_databases()
    {
        var first = NewSeededContext(out var firstPath);
        var second = NewSeededContext(out var secondPath);

        static string Fingerprint(AppDbContext db)
        {
            var seats = string.Join("|", db.Seats.OrderBy(s => s.Id)
                .Select(s => $"{s.Id}:{s.EventId}:{s.Row}{s.Number}:{s.Price}:{s.IsBooked}")
                .ToList());
            var bookings = string.Join("|", db.Bookings.OrderBy(b => b.Id)
                .Select(b => $"{b.Id}:{b.SeatId}:{b.BuyerEmail}:{b.Amount}:{b.CreatedAtUtc:O}")
                .ToList());
            return seats + "#" + bookings;
        }

        Assert.Equal(Fingerprint(first), Fingerprint(second));

        CleanUp(first, firstPath);
        CleanUp(second, secondPath);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Every_seeded_booking_has_a_matching_payment_and_audit_row()
    {
        var db = NewSeededContext(out var path);

        foreach (var booking in db.Bookings.ToList())
        {
            var payment = Assert.Single(db.Payments.Where(p => p.BookingId == booking.Id).ToList());
            var audit = Assert.Single(db.AuditEntries.Where(a => a.BookingId == booking.Id).ToList());
            Assert.Equal(booking.Amount, payment.Amount);
            Assert.Equal(booking.Amount, audit.Amount);
            Assert.Equal(booking.BuyerEmail, audit.BuyerEmail);
        }

        Assert.All(db.Seats.ToList(), s => Assert.Equal(40 + s.Row * 5, s.Price));

        CleanUp(db, path);
    }
}
