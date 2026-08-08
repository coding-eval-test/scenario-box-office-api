using BoxOfficeApi.Dtos;

namespace BoxOfficeApi.Services;

public interface IBookingService
{
    Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken);

    Task<BookingResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
}
