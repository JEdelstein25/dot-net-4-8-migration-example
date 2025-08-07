using Microsoft.AspNetCore.Mvc;

namespace TaxCalculator.Api.Controllers
{
    [RoutePrefix("api/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [Route("")]
        public IActionResult GetHealth()
        {
            return Ok(new { status = "OK", timestamp = System.DateTime.UtcNow });
        }
    }
}
