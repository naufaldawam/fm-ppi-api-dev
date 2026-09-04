using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>
    /// Data Master > RF.ID. Assignment RF.ID ke Pekerja dan Kendaraan (Nopol).
    /// Create/Update/Delete otomatis mensinkronkan ke Pekerja.RfIds.
    /// </summary>
    [ApiController]
    [Route("api/rfid")]
    [Authorize]
    [Produces("application/json")]
    public class RfIdController : ControllerBase
    {
        private readonly IRfIdService _rfIdService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<RfIdController> _logger;

        public RfIdController(
            IRfIdService rfIdService,
            ICurrentUser currentUser,
            ILogger<RfIdController> logger)
        {
            _rfIdService = rfIdService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("rfid.read")]
        public async Task<IActionResult> GetAll([FromQuery] RfIdFilterRequest filter)
        {
            var result = await _rfIdService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("rfid.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _rfIdService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("rfid.create")]
        public async Task<IActionResult> Create([FromBody] CreateRfIdRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _rfIdService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("RF.ID created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("rfid.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRfIdRequest request)
        {
            var result = await _rfIdService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("rfid.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _rfIdService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}