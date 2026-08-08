using BoxOfficeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BoxOfficeApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Venue).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.Property(s => s.Number).HasMaxLength(4).IsRequired();
            entity.HasOne(s => s.Event).WithMany(e => e.Seats)
                  .HasForeignKey(s => s.EventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(s => new { s.EventId, s.Row, s.Number }).IsUnique();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.BuyerEmail).HasMaxLength(320).IsRequired();
            entity.HasOne(b => b.Seat).WithMany(s => s.Bookings)
                  .HasForeignKey(b => b.SeatId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(b => b.SeatId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.ProviderReference).HasMaxLength(64).IsRequired();
            entity.HasOne(p => p.Booking).WithMany()
                  .HasForeignKey(p => p.BookingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => p.BookingId).IsUnique();
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.Property(a => a.EventName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.SeatLabel).HasMaxLength(8).IsRequired();
            entity.Property(a => a.BuyerEmail).HasMaxLength(320).IsRequired();
            entity.HasOne(a => a.Booking).WithMany()
                  .HasForeignKey(a => a.BookingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(a => a.BookingId).IsUnique();
        });
    }
}
