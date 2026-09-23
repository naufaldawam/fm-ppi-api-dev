using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Master data Kategori Kecelakaan (dropdown di form).</summary>
    [ApiController]
    [Route("kategori-kecelakaan")]
    [Authorize]
    [Produces("application/json")]
    public class KategoriKecelakaanController : ControllerBase
    {
        private readonly IKategoriKecelakaanService _kategoriKecelakaanService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<KategoriKecelakaanController> _logger;

        public KategoriKecelakaanController(
            IKategoriKecelakaanService kategoriKecelakaanService,
            ICurrentUser currentUser,
            ILogger<KategoriKecelakaanController> logger)
        {
            _kategoriKecelakaanService = kategoriKecelakaanService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("kategori-kecelakaan.read")]
        public async Task<IActionResult> GetAll([FromQuery] KategoriKecelakaanFilterRequest filter)
        {
            var result = await _kategoriKecelakaanService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>Lookup dropdown: GET /kategori-kecelakaan/lookup?search=xxx&amp;activeOnly=true</summary>
        [HttpGet("lookup")]
        [RequirePermission("kategori-kecelakaan.read")]
        public async Task<IActionResult> GetLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _kategoriKecelakaanService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("kategori-kecelakaan.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _kategoriKecelakaanService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("kategori-kecelakaan.create")]
        public async Task<IActionResult> Create([FromBody] CreateKategoriKecelakaanRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _kategoriKecelakaanService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Kategori kecelakaan created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("kategori-kecelakaan.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateKategoriKecelakaanRequest request)
        {
            var result = await _kategoriKecelakaanService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("kategori-kecelakaan.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _kategoriKecelakaanService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}