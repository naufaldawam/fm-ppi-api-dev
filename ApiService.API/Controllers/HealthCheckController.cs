using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ApiService.API.Controllers
{
    [ApiController]
    [Route("")]
    [Produces("application/json")]
    public class HealthCheckController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;
        private readonly ILogger<HealthCheckController> _logger;

        public HealthCheckController(
            HealthCheckService healthCheckService,
            ILogger<HealthCheckController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        /// <summary>Basic health check — is the service alive?</summary>
        [HttpGet]
        public IActionResult Ping()
        {
            return Ok(new
            {
                Status = "UP terbaru 18 06 2026",
                Service = "ApiService",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>Detailed health check — includes DB, dependencies, etc.</summary>
        [HttpGet("details")]
        public async Task<IActionResult> Details()
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var result = new
            {
                Status = report.Status == HealthStatus.Healthy ? "UP" : "DOWN",
                Service = "ApiService",
                Timestamp = DateTime.UtcNow,
                Duration = report.TotalDuration,
                Checks = report.Entries.Select(e => new
                {
                    Name = e.Key,
                    Status = e.Value.Status == HealthStatus.Healthy ? "UP" : "DOWN",
                    Description = e.Value.Description,
                    Duration = e.Value.Duration
                })
            };

            return report.Status == HealthStatus.Healthy
                ? Ok(result)
                : StatusCode(503, result);
        }
    }
}