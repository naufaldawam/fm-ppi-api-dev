using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>
    /// Data Master > Member Parkir.
    /// Field input: Pekerja, Periode, Tanggal Penagihan, Jumlah Biaya.
    /// NamaPekerja / No.Pekerja / Jabatan otomatis dari Pekerja yang dipilih.
    /// RF.ID TIDAK diinput - ditarik dari Pekerja.RfIds (bisa kosong / lebih dari satu).
    /// </summary>
    [ApiController]
    [Route("member-parkir")]
    [Authorize]
    [Produces("application/json")]
    public class MemberParkirController : ControllerBase
    {
        private readonly IMemberParkirService _memberParkirService;
        private readonly IPekerjaService _pekerjaService;
        private readonly IPeriodeService _periodeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<MemberParkirController> _logger;

        public MemberParkirController(
            IMemberParkirService memberParkirService,
            IPekerjaService pekerjaService,
            IPeriodeService periodeService,
            ICurrentUser currentUser,
            ILogger<MemberParkirController> logger)
        {
            _memberParkirService = memberParkirService;
            _pekerjaService = pekerjaService;
            _periodeService = periodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("member-parkir.read")]
        public async Task<IActionResult> GetAll([FromQuery] MemberParkirFilterRequest filter)
        {
            var result = await _memberParkirService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Dropdown Pekerja: setelah user pilih Pekerja,
        /// FE terisi NamaPekerja, No.Pekerja, Jabatan, dan daftar RfIds (kalau ada)
        /// dari response (rfIds[] - bisa kosong).
        /// GET /member-parkir/pekerja-lookup?search=xxx&activeOnly=true
        /// GET /member-parkir/pekerja-lookup?pekerjaId=xxx   (prefill satu pekerja)
        /// </summary>
        [HttpGet("pekerja-lookup")]
        [RequirePermission("member-parkir.read")]
        public async Task<IActionResult> GetPekerjaLookup(
            [FromQuery] string? pekerjaId,
            [FromQuery] string? search,
            [FromQuery] bool activeOnly = true)
        {
            if (!string.IsNullOrWhiteSpace(pekerjaId))
            {
                var byIdResult = await _memberParkirService.GetPekerjaByPekerjaIdAsync(pekerjaId);
                return StatusCode(byIdResult.StatusCode, byIdResult);
            }

            var result = await _pekerjaService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Dropdown Periode: pilih periode di mana record Member Parkir ini dibuat.
        /// GET /member-parkir/periode-lookup?search=xxx&activeOnly=false
        /// </summary>
        [HttpGet("periode-lookup")]
        [RequirePermission("member-parkir.read")]
        public async Task<IActionResult> GetPeriodeLookup([FromQuery] string? search, [FromQuery] bool activeOnly = false)
        {
            var result = await _periodeService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("member-parkir.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _memberParkirService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("member-parkir.create")]
        public async Task<IActionResult> Create([FromBody] CreateMemberParkirRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _memberParkirService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Member parkir created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("member-parkir.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateMemberParkirRequest request)
        {
            var result = await _memberParkirService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("member-parkir.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _memberParkirService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Download template Excel untuk bulk upload Member Parkir.</summary>
        [HttpGet("download-template")]
        [RequirePermission("member-parkir.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template Member Parkir.", _currentUser.UserId);

            var result = await _memberParkirService.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.FileStream, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Bulk upload data Member Parkir dari file Excel (.xlsx).
        /// Kolom wajib: NoPekerja | Periode | TanggalPenagihan | JumlahBiaya
        /// RF.ID TIDAK ada di template - selalu ditarik dari Pekerja.RfIds.
        /// Gunakan endpoint download-template untuk mendapatkan file template beserta sheet referensi.
        /// </summary>
        [HttpPost("import-excel")]
        [RequirePermission("member-parkir.create")]
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
            var result = await _memberParkirService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import Member Parkir | File: {FileName} | Size: {FileSize} | UserId: {UserId} | Success: {Success}",
                file.FileName, file.Length, userId, result.Success);

            return StatusCode(result.StatusCode, result);
        }
    }
}