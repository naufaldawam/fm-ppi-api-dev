using Microsoft.EntityFrameworkCore;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;
using ApiService.Application.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ApiService.Application.DTOs.Configs;
using System.Net.Http.Json;
using ClosedXML.Excel;

namespace ApiService.Application.Services
{
    public interface IFeedbackService
    {
        Task<ApiResponse<bool>> SubmitFeedbackAsync(CreateFeedbackRequest request);
        Task<ApiResponse<bool>> SkipFeedbackAsync();
        Task<ApiResponse<FeedbackSummaryResponse>> GetReportingSummaryAsync();
        Task<ApiResponse<PagedResponse<FeedbackResponse>>> GetFeedbackAsync(FeedbackFilterRequest request);
        Task<byte[]> ExportFeedbackToExcelAsync(FeedbackFilterRequest request);
    }

    public class FeedbackService(
        IServiceDbContext context,
        ICurrentUser currentUser,
        IConfiguration config,
        ILogger<FeedbackService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<CallbackConfig> callbackConfig) : IFeedbackService
    {
        private readonly IServiceDbContext _context = context;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly IConfiguration _config = config;
        private readonly ILogger _logger = logger;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly CallbackConfig _callbackConfig = callbackConfig.Value;

        public async Task<ApiResponse<bool>> SubmitFeedbackAsync(CreateFeedbackRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.UserId == _currentUser.UserId);

                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        "404",
                        "User tidak ditemukan."
                    );
                }

                var feedback = new FeedbackEntity
                {
                    UserId = user.UserId,
                    UserName = user.Name,
                    UserEmail = user.Email,

                    Rating = request.Rating,
                    Comment = request.Comment,

                    CreatedBy = user.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                // Update Status Feedback di Auth Service
                var client = _httpClientFactory.CreateClient();
                var callbackUrl = _callbackConfig.CallbackFeedback;

                var response = await client.PutAsJsonAsync(
                    $"{callbackUrl}/auth/feedback-status",
                    new UpdateFeedbackStatusRequest
                    {
                        UserId = _currentUser.UserId!
                    });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed Update Feedback Status | UserId: {UserId}",
                        _currentUser.UserId);

                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("AuthService Response: {Error}", error);
                }


                return ApiResponse<bool>.SuccessResponse(
                    true,
                    "Feedback berhasil dikirim."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Submit Feedback Failed");

                return ApiResponse<bool>.ErrorResponse(
                    "500",
                    ex.Message
                );
            }
        }

        public async Task<ApiResponse<bool>> SkipFeedbackAsync()
        {
            try
            {
                return ApiResponse<bool>.SuccessResponse(
                    true,
                    "Feedback berhasil dilewati."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skip Feedback Failed");

                return ApiResponse<bool>.ErrorResponse(
                    "500",
                    ex.Message
                );
            }
        }

        public async Task<ApiResponse<FeedbackSummaryResponse>> GetReportingSummaryAsync()
        {
            var feedbacks = await _context.Feedbacks
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var currentMonth = feedbacks
                .Where(x => x.CreatedAt.Year == now.Year &&
                            x.CreatedAt.Month == now.Month)
                .ToList();

            var previousMonthDate = now.AddMonths(-1);

            var previousMonth = feedbacks
                .Where(x => x.CreatedAt.Year == previousMonthDate.Year &&
                            x.CreatedAt.Month == previousMonthDate.Month)
                .ToList();

            var summary = new FeedbackSummaryResponse
            {
                TotalFeedback = feedbacks.Count,

                AverageRating = feedbacks.Any()
                    ? Math.Round(feedbacks.Average(x => (decimal)x.Rating), 1)
                    : 0m,

                RatingTrendThisMonth = currentMonth.Any()
                    ? (int)Math.Round(currentMonth.Average(x => x.Rating))
                    : 0,

                RatingTrendLastMonth = previousMonth.Any()
                    ? (int)Math.Round(previousMonth.Average(x => x.Rating))
                    : 0
            };

            return ApiResponse<FeedbackSummaryResponse>.SuccessResponse(
                summary,
                "Feedback summary loaded successfully.");
        }

        public async Task<ApiResponse<PagedResponse<FeedbackResponse>>> GetFeedbackAsync(FeedbackFilterRequest request)
        {
            var query = _context.Feedbacks
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (request.Rating.HasValue)
            {
                query = query.Where(x => x.Rating == request.Rating.Value);
            }

            if (request.WithComment.HasValue)
            {
                if (request.WithComment.Value)
                {
                    query = query.Where(x => !string.IsNullOrWhiteSpace(x.Comment));
                }
                else
                {
                    query = query.Where(x => string.IsNullOrWhiteSpace(x.Comment));
                }
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var keyword = request.SearchTerm.Trim().ToLower();

                query = query.Where(x =>
                    x.UserName.ToLower().Contains(keyword) ||
                    (x.Comment != null && x.Comment.ToLower().Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.Page - 1) * request.Size)
                .Take(request.Size)
                .Select(x => new FeedbackResponse
                {
                    Id = x.Id,
                    CreatedAt = x.CreatedAt,
                    Rating = x.Rating,
                    Comment = x.Comment,
                    UserName = x.UserName
                })
                .ToListAsync();

            var response = new PagedResponse<FeedbackResponse>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.Size
            };

            return ApiResponse<PagedResponse<FeedbackResponse>>
                .SuccessResponse(response, "Feedback berhasil dimuat.");
        }

        public async Task<byte[]> ExportFeedbackToExcelAsync(FeedbackFilterRequest request)
        {
            var query = _context.Feedbacks
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (request.Rating.HasValue)
            {
                query = query.Where(x => x.Rating == request.Rating.Value);
            }

            if (request.WithComment.HasValue)
            {
                if (request.WithComment.Value)
                {
                    query = query.Where(x => !string.IsNullOrWhiteSpace(x.Comment));
                }
                else
                {
                    query = query.Where(x => string.IsNullOrWhiteSpace(x.Comment));
                }
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var keyword = request.SearchTerm.Trim().ToLower();

                query = query.Where(x =>
                    x.UserName.ToLower().Contains(keyword) ||
                    (x.Comment != null && x.Comment.ToLower().Contains(keyword)));
            }

            var data = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Feedback");

            ws.Cell(1, 1).Value = "No";
            ws.Cell(1, 2).Value = "Date";
            ws.Cell(1, 3).Value = "Rating";
            ws.Cell(1, 4).Value = "Comment";
            ws.Cell(1, 5).Value = "User";

            var header = ws.Range(1, 1, 1, 5);
            header.Style.Font.Bold = true;

            int row = 2;
            int no = 1;

            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.CreatedAt.ToString("dd MMM yyyy HH:mm");
                ws.Cell(row, 3).Value = item.Rating;
                ws.Cell(row, 4).Value = item.Comment;
                ws.Cell(row, 5).Value = item.UserName;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}