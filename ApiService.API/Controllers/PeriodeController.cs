using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Data Master > Periode</summary>
    [ApiController]
    [Route("api/periode")]
    [Authorize]
    [Produces("application/json")]
    public class PeriodeController : ControllerBase
    {
        private readonly IPeriodeService _periodeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<PeriodeController> _logger;

        public PeriodeController(
            IPeriodeService periodeService,
            ICurrentUser currentUser,
            ILogger<PeriodeController> logger)
        {
            _periodeService = periodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        /// <summary>Get all periode (paginated + filtered)</summary>
        [HttpGet]
        [RequirePermission("periode.read")]
        public async Task<IActionResult> GetAll([FromQuery] PeriodeFilterRequest filter)
        {
            var result = await _periodeService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>Get periode by ID</summary>
        [HttpGet("{id}")]
        [RequirePermission("periode.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _periodeService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>Tambah periode baru (Tambah Periode)</summary>
        [HttpPost]
        [RequirePermission("periode.create")]
        public async Task<IActionResult> Create([FromBody] CreatePeriodeRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _periodeService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Periode created by user {UserId}", userId);
            return Ok(result);
        }

        /// <summary>Edit periode (termasuk status aktif/non-aktif)</summary>
        [HttpPut("{id}")]
        [RequirePermission("periode.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePeriodeRequest request)
        {
            var result = await _periodeService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>Hapus periode (soft delete)</summary>
        [HttpDelete("{id}")]
        [RequirePermission("periode.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _periodeService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }
    }
}