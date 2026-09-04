using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Data Master > Driver. Terhubung ke Vendor dan Atasan (Pekerja).</summary>
    [ApiController]
    [Route("api/driver")]
    [Authorize]
    [Produces("application/json")]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<DriverController> _logger;

        public DriverController(
            IDriverService driverService,
            ICurrentUser currentUser,
            ILogger<DriverController> logger)
        {
            _driverService = driverService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("driver.read")]
        public async Task<IActionResult> GetAll([FromQuery] DriverFilterRequest filter)
        {
            var result = await _driverService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("driver.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _driverService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("driver.create")]
        public async Task<IActionResult> Create([FromBody] CreateDriverRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _driverService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Driver created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("driver.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateDriverRequest request)
        {
            var result = await _driverService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("driver.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _driverService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}