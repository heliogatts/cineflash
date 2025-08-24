using Microsoft.AspNetCore.Mvc;

namespace CloudFlash.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Endpoint de health check básico
    /// </summary>
    /// <returns>Status da aplicação</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            service = "CloudFlash API"
        });
    }
}
