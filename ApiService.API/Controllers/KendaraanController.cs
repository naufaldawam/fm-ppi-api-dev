using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>
    /// Data Master > Kendaraan. Referensi ke Tipe, Bahan Bakar, Vendor, Kepemilikan,
    /// Jabatan (alokasi jabatan) dan Pekerja (pejabat pemegang - opsional).
    /// </summary>
    [ApiController]
    [Route("api/kendaraan")]
    [Authorize]
    [Produces("application/json")]
    public class KendaraanController : ControllerBase
    {
        private readonly IKendaraanService _kendaraanService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<KendaraanController> _logger;

        public KendaraanController(
            IKendaraanService kendaraanService,
            ICurrentUser currentUser,
            ILogger<KendaraanController> logger)
        {
            _kendaraanService = kendaraanService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("kendaraan.read")]
        public async Task<IActionResult> GetAll([FromQuery] KendaraanFilterRequest filter)
        {
            var result = await _kendaraanService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("lookup")]
        [RequirePermission("kendaraan.read")]
        public async Task<IActionResult> GetLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _kendaraanService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("kendaraan.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _kendaraanService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("kendaraan.create")]
        public async Task<IActionResult> Create([FromBody] CreateKendaraanRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _kendaraanService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Kendaraan created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("kendaraan.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateKendaraanRequest request)
        {
            var result = await _kendaraanService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("kendaraan.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _kendaraanService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}