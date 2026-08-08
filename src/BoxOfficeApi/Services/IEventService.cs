using BoxOfficeApi.Dtos;

namespace BoxOfficeApi.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventResponse>> ListAsync(CancellationToken cancellationToken);

    Task<SeatMapResponse> GetSeatMapAsync(int eventId, CancellationToken cancellationToken);
}
