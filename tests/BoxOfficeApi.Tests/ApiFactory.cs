using BoxOfficeApi.Data;
using BoxOfficeApi.Services.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace BoxOfficeApi.Tests;

/// <summary>
/// Boots the real API pipeline against a private, freshly seeded SQLite file,
/// with the clock and the payment gateway replaced by deterministic test
/// doubles. One instance per test class via IClassFixture.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"box-office-tests-{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={_dbPath}";

    /// <summary>
    /// The suite's clock: 2026-03-01T12:00:00Z, local time zone UTC. Events 1-2
    /// are on sale at this instant; event 3 is not. Tests move it with
    /// Advance/SetUtcNow/SetLocalTimeZone.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Counting, failable gateway shared by every request this factory serves.</summary>
    public TestPaymentGateway Gateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(ConnectionString));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(Gateway);
        });
    }

    /// <summary>A context pointing at the same database the API under test uses.</summary>
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Best effort — a leaked temp file must not fail the suite.
            }
        }
    }
}
