using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiService.Application.Interfaces;
using ApiService.Application.DTOs;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.Presentation.Controllers
{
    /// <summary>
    /// FEEDBACK CONTROLLER — iProm Project.
    /// Manages order creation and retrieval with permission checks.
    /// </summary>
    [ApiController]
    [Route("feedback")]
    [Authorize]
    [Produces("application/json")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(
            IFeedbackService feedbackService,
            ICurrentUser currentUser,
            ILogger<FeedbackController> logger)
        {
            _feedbackService = feedbackService;
            _currentUser = currentUser;
            _logger = logger;
        }
        [HttpPost]
        [RequirePermission("feedback.create")]
        public async Task<IActionResult> SubmitFeedback([FromBody] CreateFeedbackRequest request)
        {
            _logger.LogInformation(
                "Submit Feedback | UserId: {UserId}",
                _currentUser.UserId
            );

            var result = await _feedbackService.SubmitFeedbackAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("skip")]
        [RequirePermission("feedback.read")]
        public async Task<IActionResult> SkipFeedback()
        {
            _logger.LogInformation(
                "Skip Feedback | UserId: {UserId}",
                _currentUser.UserId
            );

            var result = await _feedbackService.SkipFeedbackAsync();

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpGet("reporting-cards")]
        [RequirePermission("feedback.read")]
        public async Task<IActionResult> GetCards()
        {
            var result = await _feedbackService.GetReportingSummaryAsync();
            return Ok(result);
        }

        [HttpGet("list")]
        [RequirePermission("feedback.read")]
        public async Task<IActionResult> GetFeedbackList([FromQuery] FeedbackFilterRequest request)
        {
            var result = await _feedbackService.GetFeedbackAsync(request);
            return Ok(result);
        }

        [HttpGet("export")]
        [RequirePermission("feedback.read")]
        public async Task<IActionResult> ExportFeedback([FromQuery] FeedbackFilterRequest request)
        {
            var fileBytes = await _feedbackService.ExportFeedbackToExcelAsync(request);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Feedback_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

    }
}