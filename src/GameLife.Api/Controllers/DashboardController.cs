using gamelife_api.Contracts;
using gamelife_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace gamelife_api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet("inicio")]
    public Task<DashboardInicioResponse> ObterInicio(CancellationToken cancellationToken)
        => service.ObterInicioAsync(cancellationToken);
}
