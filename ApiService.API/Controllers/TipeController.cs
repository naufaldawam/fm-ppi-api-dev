using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Master data Tipe (dropdown di form Kendaraan)</summary>
    [ApiController]
    [Route("api/tipe")]
    [Authorize]
    [Produces("application/json")]
    public class TipeController : ControllerBase
    {
        private readonly ITipeService _tipeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<TipeController> _logger;

        public TipeController(
            ITipeService tipeService,
            ICurrentUser currentUser,
            ILogger<TipeController> logger)
        {
            _tipeService = tipeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("tipe.read")]
        public async Task<IActionResult> GetAll([FromQuery] TipeFilterRequest filter)
        {
            var result = await _tipeService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("lookup")]
        [RequirePermission("tipe.read")]
        public async Task<IActionResult> GetLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _tipeService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("tipe.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _tipeService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("tipe.create")]
        public async Task<IActionResult> Create([FromBody] CreateTipeRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _tipeService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Tipe created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("tipe.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTipeRequest request)
        {
            var result = await _tipeService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("tipe.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _tipeService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}