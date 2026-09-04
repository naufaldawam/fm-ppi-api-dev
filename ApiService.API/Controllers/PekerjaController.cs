using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Data Master > Pekerja (master independen)</summary>
    [ApiController]
    [Route("pekerja")]
    [Authorize]
    [Produces("application/json")]
    public class PekerjaController : ControllerBase
    {
        private readonly IPekerjaService _pekerjaService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<PekerjaController> _logger;

        public PekerjaController(
            IPekerjaService pekerjaService,
            ICurrentUser currentUser,
            ILogger<PekerjaController> logger)
        {
            _pekerjaService = pekerjaService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("pekerja.read")]
        public async Task<IActionResult> GetAll([FromQuery] PekerjaFilterRequest filter)
        {
            var result = await _pekerjaService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("lookup")]
        [RequirePermission("pekerja.read")]
        public async Task<IActionResult> GetLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _pekerjaService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("pekerja.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _pekerjaService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("pekerja.create")]
        public async Task<IActionResult> Create([FromBody] CreatePekerjaRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _pekerjaService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Pekerja created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("pekerja.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePekerjaRequest request)
        {
            var result = await _pekerjaService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("pekerja.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _pekerjaService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }
    }
}