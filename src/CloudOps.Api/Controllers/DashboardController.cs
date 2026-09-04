using CloudOps.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudOps.Api.Controllers;

[ApiController, Authorize, Route("api/dashboard")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken) => Ok(await dashboard.GetAsync(cancellationToken));
}
