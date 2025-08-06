using System.Web.Http;

namespace TaxCalculator.Api.Controllers
{
    [RoutePrefix("api/health")]
    public class HealthController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetHealth()
        {
            return Ok(new { status = "OK", timestamp = System.DateTime.UtcNow });
        }
    }
}
