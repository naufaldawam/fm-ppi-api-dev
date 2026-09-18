using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Master data Jenis BBM (dropdown di form).</summary>
    [ApiController]
    [Route("jenis-bbm")]
    [Authorize]
    [Produces("application/json")]
    public class JenisBbmController : ControllerBase
    {
        private readonly IJenisBbmService _jenisBbmService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<JenisBbmController> _logger;

        public JenisBbmController(
            IJenisBbmService jenisBbmService,
            ICurrentUser currentUser,
            ILogger<JenisBbmController> logger)
        {
            _jenisBbmService = jenisBbmService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("jenis-bbm.read")]
        public async Task<IActionResult> GetAll([FromQuery] JenisBbmFilterRequest filter)
        {
            var result = await _jenisBbmService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>Lookup dropdown: GET /jenis-bbm/lookup?search=xxx&amp;activeOnly=true</summary>
        [HttpGet("lookup")]
        [RequirePermission("jenis-bbm.read")]
        public async Task<IActionResult> GetLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _jenisBbmService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("jenis-bbm.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _jenisBbmService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("jenis-bbm.create")]
        public async Task<IActionResult> Create([FromBody] CreateJenisBbmRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _jenisBbmService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Jenis BBM created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("jenis-bbm.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateJenisBbmRequest request)
        {
            var result = await _jenisBbmService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("jenis-bbm.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _jenisBbmService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}