using BoxOfficeApi.Dtos;
using BoxOfficeApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BoxOfficeApi.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;

    public EventsController(IEventService events) => _events = events;

    /// <summary>Lists every event with its seat availability counts.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> List(CancellationToken cancellationToken)
        => Ok(await _events.ListAsync(cancellationToken));

    /// <summary>Gets the seat map for one event, with per-seat availability.</summary>
    [HttpGet("{id:int}/seats")]
    [ProducesResponseType(typeof(SeatMapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeatMapResponse>> SeatMap(int id, CancellationToken cancellationToken)
        => Ok(await _events.GetSeatMapAsync(id, cancellationToken));
}
