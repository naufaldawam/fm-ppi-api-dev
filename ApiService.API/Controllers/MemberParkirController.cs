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
    /// Field: NamaPekerja, Rfid, No.Pekerja, Jabatan (auto-fill dari Pekerja),
    /// Tanggal Penagihan, Jumlah Biaya.
    /// </summary>
    [ApiController]
    [Route("member-parkir")]
    [Authorize]
    [Produces("application/json")]
    public class MemberParkirController : ControllerBase
    {
        private readonly IMemberParkirService _memberParkirService;
        private readonly IPekerjaService _pekerjaService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<MemberParkirController> _logger;

        public MemberParkirController(
            IMemberParkirService memberParkirService,
            IPekerjaService pekerjaService,
            ICurrentUser currentUser,
            ILogger<MemberParkirController> logger)
        {
            _memberParkirService = memberParkirService;
            _pekerjaService = pekerjaService;
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
        /// FE terisi NamaPekerja, No.Pekerja, Jabatan, dan Rfid
        /// dari response (rfIds[]).
        /// GET /member-parkir/pekerja-lookup?search=xxx&activeOnly=true
        /// </summary>
        [HttpGet("pekerja-lookup")]
        [RequirePermission("member-parkir.read")]
        public async Task<IActionResult> GetPekerjaLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _pekerjaService.GetLookupAsync(search, activeOnly);
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
    }
}