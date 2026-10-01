using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;
using ApiService.Domain.Entities;

namespace ApiService.API.Controllers
{
    /// <summary>
    /// Tagihan KWh - khusus kategori P8. Endpoint diprefiks /p8.
    /// Kelak non-P8 ditulis di controller ini juga (route group yang sama,
    /// beda kategori), pakai service yang sama.
    /// </summary>
    [ApiController]
    [Route("tagihan-kwh")]
    [Authorize]
    [Produces("application/json")]
    public class TagihanKwhController : ControllerBase
    {
        private readonly ITagihanKwhService _tagihanKwhService;
        private readonly IPekerjaService _pekerjaService;
        private readonly IPeriodeService _periodeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<TagihanKwhController> _logger;

        public TagihanKwhController(
            ITagihanKwhService tagihanKwhService,
            IPekerjaService pekerjaService,
            IPeriodeService periodeService,
            ICurrentUser currentUser,
            ILogger<TagihanKwhController> logger)
        {
            _tagihanKwhService = tagihanKwhService;
            _pekerjaService = pekerjaService;
            _periodeService = periodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet("p8")]
        [RequirePermission("tagihan-kwh.read")]
        public async Task<IActionResult> GetAll([FromQuery] TagihanKwhFilterRequest filter)
        {
            var result = await _tagihanKwhService.GetAllAsync(filter, TagihanKwh.KategoriP8);
            return Ok(result);
        }

        /// <summary>
        /// Dropdown Pekerja: GET /p8/tagihan-kwh/pekerja-lookup?search=xxx&activeOnly=true
        /// </summary>
        [HttpGet("p8/pekerja-lookup")]
        [RequirePermission("tagihan-kwh.read")]
        public async Task<IActionResult> GetPekerjaLookup(
            [FromQuery] string? search,
            [FromQuery] string? jabatanId,
            [FromQuery] bool activeOnly = true)
        {
            var result = await _pekerjaService.GetLookupAsync(search, jabatanId, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Dropdown Periode: GET /p8/tagihan-kwh/periode-lookup?search=xxx&activeOnly=false
        /// </summary>
        [HttpGet("p8/periode-lookup")]
        [RequirePermission("tagihan-kwh.read")]
        public async Task<IActionResult> GetPeriodeLookup([FromQuery] string? search, [FromQuery] bool activeOnly = false)
        {
            var result = await _periodeService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("p8/{id}")]
        [RequirePermission("tagihan-kwh.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _tagihanKwhService.GetByIdAsync(id, TagihanKwh.KategoriP8);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost("p8")]
        [RequirePermission("tagihan-kwh.create")]
        public async Task<IActionResult> Create([FromBody] CreateTagihanKwhRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _tagihanKwhService.CreateAsync(request, TagihanKwh.KategoriP8, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Tagihan KWh (P8) created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("p8/{id}")]
        [RequirePermission("tagihan-kwh.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTagihanKwhRequest request)
        {
            var result = await _tagihanKwhService.UpdateAsync(
                id, request, TagihanKwh.KategoriP8, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("p8/{id}")]
        [RequirePermission("tagihan-kwh.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _tagihanKwhService.DeleteAsync(id, TagihanKwh.KategoriP8, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Summary buat box atas: Total Member, Grand Total Biaya, Grand Total KWh (khusus P8).</summary>
        [HttpGet("p8/summary")]
        [RequirePermission("tagihan-kwh.read")]
        public async Task<IActionResult> GetSummary([FromQuery] TagihanKwhSummaryRequest filter)
        {
            var result = await _tagihanKwhService.GetSummaryAsync(filter, TagihanKwh.KategoriP8);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("p8/download-template")]
        [RequirePermission("tagihan-kwh.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template Tagihan KWh.", _currentUser.UserId);

            var result = await _tagihanKwhService.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.FileStream, file.ContentType, file.FileName);
        }

        [HttpPost("p8/import-excel")]
        [RequirePermission("tagihan-kwh.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
                return BadRequest(new { success = false, message = "Hanya mendukung file dengan format .xlsx." });

            var userId = _currentUser.UserId!;

            await using var stream = file.OpenReadStream();
            var result = await _tagihanKwhService.ImportFromExcelAsync(stream, TagihanKwh.KategoriP8, userId);

            _logger.LogInformation(
                "Import Tagihan KWh (P8) | File: {FileName} | Size: {FileSize} | UserId: {UserId} | Success: {Success}",
                file.FileName, file.Length, userId, result.Success);

            return StatusCode(result.StatusCode, result);
        }
    }
}