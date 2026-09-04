using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Master data Jabatan (dropdown di form Pekerja)</summary>
    [ApiController]
    [Route("api/jabatan")]
    [Authorize]
    [Produces("application/json")]
    public class JabatanController : ControllerBase
    {
        private readonly IJabatanService _jabatanService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<JabatanController> _logger;

        public JabatanController(
            IJabatanService jabatanService,
            ICurrentUser currentUser,
            ILogger<JabatanController> logger)
        {
            _jabatanService = jabatanService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("jabatan.read")]
        public async Task<IActionResult> GetAll([FromQuery] JabatanFilterRequest filter)
        {
            var result = await _jabatanService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("jabatan.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _jabatanService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("jabatan.create")]
        public async Task<IActionResult> Create([FromBody] CreateJabatanRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _jabatanService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Jabatan created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("jabatan.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateJabatanRequest request)
        {
            var result = await _jabatanService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("jabatan.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _jabatanService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}