using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Master data BahanBakar (dropdown di form Kendaraan)</summary>
    [ApiController]
    [Route("api/bahanbakar")]
    [Authorize]
    [Produces("application/json")]
    public class BahanBakarController : ControllerBase
    {
        private readonly IBahanBakarService _bahanBakarService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<BahanBakarController> _logger;

        public BahanBakarController(
            IBahanBakarService bahanBakarService,
            ICurrentUser currentUser,
            ILogger<BahanBakarController> logger)
        {
            _bahanBakarService = bahanBakarService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("bahanbakar.read")]
        public async Task<IActionResult> GetAll([FromQuery] BahanBakarFilterRequest filter)
        {
            var result = await _bahanBakarService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("bahanbakar.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _bahanBakarService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("bahanbakar.create")]
        public async Task<IActionResult> Create([FromBody] CreateBahanBakarRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _bahanBakarService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("BahanBakar created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("bahanbakar.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateBahanBakarRequest request)
        {
            var result = await _bahanBakarService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("bahanbakar.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _bahanBakarService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}