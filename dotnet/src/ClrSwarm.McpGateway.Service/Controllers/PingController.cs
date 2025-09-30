
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClrSwarm.McpGateway.Service.Controllers;

[ApiController]
[Route("ping")]
[AllowAnonymous]
public class PingController : Controller
{
    // GET /ping
    [HttpGet]
    public IActionResult Ping() => Ok();
}
