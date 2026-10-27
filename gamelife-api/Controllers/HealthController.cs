using gamelife_api.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gamelife_api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly GameLifeDbContext _contexto;

    public HealthController(GameLifeDbContext contexto)
    {
        _contexto = contexto;
    }

    [HttpGet]
    public async Task<IActionResult> ObterStatus(CancellationToken cancellationToken)
    {
        if (!await _contexto.Database.CanConnectAsync(cancellationToken))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                application = "GameLife",
                status = "database-unavailable"
            });
        }

        return Ok(new { application = "GameLife", status = "ready" });
    }
}
