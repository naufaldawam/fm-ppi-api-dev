using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Master data Kepemilikan (dropdown di form Kendaraan)</summary>
    [ApiController]
    [Route("api/kepemilikan")]
    [Authorize]
    [Produces("application/json")]
    public class KepemilikanController : ControllerBase
    {
        private readonly IKepemilikanService _kepemilikanService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<KepemilikanController> _logger;

        public KepemilikanController(
            IKepemilikanService kepemilikanService,
            ICurrentUser currentUser,
            ILogger<KepemilikanController> logger)
        {
            _kepemilikanService = kepemilikanService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("kepemilikan.read")]
        public async Task<IActionResult> GetAll([FromQuery] KepemilikanFilterRequest filter)
        {
            var result = await _kepemilikanService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("lookup")]
        [RequirePermission("kepemilikan.read")]
        public async Task<IActionResult> GetLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _kepemilikanService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("kepemilikan.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _kepemilikanService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("kepemilikan.create")]
        public async Task<IActionResult> Create([FromBody] CreateKepemilikanRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _kepemilikanService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Kepemilikan created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("kepemilikan.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateKepemilikanRequest request)
        {
            var result = await _kepemilikanService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("kepemilikan.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _kepemilikanService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}