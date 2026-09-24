using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    [ApiController]
    [Route("operasional-upah")]
    [Authorize]
    [Produces("application/json")]
    public class OperasionalUpahController : ControllerBase
    {
        private readonly IOperasionalUpahService _service;
        private readonly IPekerjaService _pekerjaService;
        private readonly IPeriodeService _periodeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<OperasionalUpahController> _logger;

        public OperasionalUpahController(
            IOperasionalUpahService service,
            IPekerjaService pekerjaService,
            IPeriodeService periodeService,
            ICurrentUser currentUser,
            ILogger<OperasionalUpahController> logger)
        {
            _service = service;
            _pekerjaService = pekerjaService;
            _periodeService = periodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        // GET /operasional-upah
        // Datatable list dengan filter Periode, Jabatan, Search, IsActive.
        [HttpGet]
        [RequirePermission("operasional-upah.read")]
        public async Task<IActionResult> GetAll([FromQuery] OperasionalUpahFilterRequest filter)
        {
            var result = await _service.GetAllAsync(filter);
            return Ok(result);
        }

        // GET /operasional-upah/summary?periodeId=xxx&isActive=true
        // Dashboard "Summary Operasional dan Upah Driver" - bisa difilter per Periode.
        // GrandTotal sudah termasuk Total BBM Approved. Info BBM Pending juga disertakan.
        [HttpGet("summary")]
        [RequirePermission("operasional-upah.read")]
        public async Task<IActionResult> GetSummary([FromQuery] OperasionalUpahSummaryRequest filter)
        {
            var result = await _service.GetSummaryAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        // GET /operasional-upah/{id}
        [HttpGet("{id}")]
        [RequirePermission("operasional-upah.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        // GET /operasional-upah/pekerja-lookup?pekerjaId=xxx
        // Prefill form: FE panggil ini setelah dropdown Pekerja dipilih untuk mengisi
        // otomatis Jabatan, RfIds, NoPekerja, Nopek Home/Host dari data Pekerja.
        // Kalau tidak ada pekerjaId, kembalikan daftar lookup (untuk dropdown Pekerja).
        [HttpGet("pekerja-lookup")]
        [RequirePermission("operasional-upah.read")]
        public async Task<IActionResult> GetPekerjaLookup(
            [FromQuery] string? pekerjaId,
            [FromQuery] string? search,
            [FromQuery] bool activeOnly = true)
        {
            if (!string.IsNullOrWhiteSpace(pekerjaId))
            {
                var prefill = await _service.GetPekerjaByIdAsync(pekerjaId);
                return StatusCode(prefill.StatusCode, prefill);
            }

            var list = await _pekerjaService.GetLookupAsync(search, activeOnly);
            return StatusCode(list.StatusCode, list);
        }

        // GET /operasional-upah/periode-lookup?search=xxx&activeOnly=false
        // Dropdown Periode untuk form Tambah/Edit dan filter datatable.
        [HttpGet("periode-lookup")]
        [RequirePermission("operasional-upah.read")]
        public async Task<IActionResult> GetPeriodeLookup(
            [FromQuery] string? search,
            [FromQuery] bool activeOnly = false)
        {
            var result = await _periodeService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        // POST /operasional-upah
        // Tambah Operasional & Upah baru. JabatanId otomatis dari Pekerja - jangan
        // dikirim dari client.
        [HttpPost]
        [RequirePermission("operasional-upah.create")]
        public async Task<IActionResult> Create([FromBody] CreateOperasionalUpahRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _service.CreateAsync(request, userId);

            if (!result.Success) return StatusCode(result.StatusCode, result);

            _logger.LogInformation(
                "OperasionalUpah created for Pekerja {PekerjaId} Periode {PeriodeId} by user {UserId}",
                request.PekerjaId, request.PeriodeId, userId);

            return Ok(result);
        }

        // PUT /operasional-upah/{id}
        [HttpPut("{id}")]
        [RequirePermission("operasional-upah.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateOperasionalUpahRequest request)
        {
            var result = await _service.UpdateAsync(id, request, _currentUser.UserId!);
            return StatusCode(result.StatusCode, result);
        }

        // DELETE /operasional-upah/{id}
        [HttpDelete("{id}")]
        [RequirePermission("operasional-upah.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id, _currentUser.UserId!);
            return StatusCode(result.StatusCode, result);
        }

        // GET /operasional-upah/download-template
        // Download template Excel untuk bulk upload (3 sheet: data, ref Pekerja, ref Periode).
        [HttpGet("download-template")]
        [RequirePermission("operasional-upah.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template OperasionalUpah.", _currentUser.UserId);

            var result = await _service.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            return File(result.Data.FileStream, result.Data.ContentType, result.Data.FileName);
        }

        // POST /operasional-upah/import-excel
        // Bulk upload dari file .xlsx. Kolom: NoPekerja | Periode |
        // TotalLembur | TotalEMoneyMember | DanaOps | TotalParkir |
        // TotalSewaKendaraan | TotalUpahDriver.
        // Kolom nominal opsional (kosong = 0). All-or-nothing: ada 1 error pun
        // tidak ada yang disimpan.
        [HttpPost("import-excel")]
        [RequirePermission("operasional-upah.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Hanya mendukung file .xlsx." });

            var userId = _currentUser.UserId!;

            await using var stream = file.OpenReadStream();
            var result = await _service.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import OperasionalUpah | File: {FileName} | Size: {Size} | UserId: {UserId} | Inserted: {Count}",
                file.FileName, file.Length, userId, result.Data?.InsertedOperasionalUpah ?? 0);

            return StatusCode(result.StatusCode, result);
        }
    }
}