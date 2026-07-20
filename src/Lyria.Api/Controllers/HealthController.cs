using Microsoft.AspNetCore.Mvc;

namespace Lyria.Api.Controllers;

[ApiController]
[Route("api/health")]
[Tags("Salud")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Comprueba que la API se encuentra disponible.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy"
        });
    }
}
