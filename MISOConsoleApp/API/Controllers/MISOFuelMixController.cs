using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MISOQueryingApp.Repository;

namespace MISOQueryingApp.API.Controllers;

[ApiController]
[Route("api/miso-fuel-mix")]
public class MISOFuelMixController : ControllerBase
{
    private readonly AppDbContext _db;

    public MISOFuelMixController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] string? category, CancellationToken cancellationToken)
    {
        if (from.HasValue && !to.HasValue)
        {
            return BadRequest("The 'to' date is missing from the interval.");
        }

        if (to.HasValue && !from.HasValue)
        {
            return BadRequest("The 'from' date is missing from the interval.");
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            return BadRequest("The 'from' date must be before the 'to' date.");
        }

        var query = _db.FuelMixSnapshots.AsNoTracking().Include(x => x.FuelMixElements).AsQueryable();

        if (from.HasValue && to.HasValue)
        {
            query = query.Where(x => x.IntervalEst >= from.Value && x.IntervalEst <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Include(x => x.FuelMixElements.Where(v => v.Category == category));
        }

        var snapshots = await query.OrderBy(x => x.IntervalEst).ToListAsync(cancellationToken);

        var result = snapshots.Select(snapshot => new
        {
            snapshot.IntervalEst,
            snapshot.TotalMegaWatts,
            Fuel = snapshot.FuelMixElements.Select(element => new
            {
                element.Category,
                element.MegaWatts
            })
        });

        return Ok(result);
    }
}
