using Microsoft.AspNetCore.Mvc;

namespace EShoppingZone.Product.API.Controllers
{
    [ApiController]
    [Route("")]
    public class HealthController : ControllerBase
    {
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(
                new
                {
                    status = "healthy",
                    service = "product-service",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                }
            );
        }

        [HttpGet("health/ready")]
        public IActionResult Ready()
        {
            return Ok(new { status = "ready", service = "product-service" });
        }

        [HttpGet("health/live")]
        public IActionResult Live()
        {
            return Ok(new { status = "alive", service = "product-service" });
        }
    }
}
