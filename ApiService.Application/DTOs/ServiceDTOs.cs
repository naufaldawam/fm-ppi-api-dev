using System;
using System.Collections.Generic;

namespace ApiService.Application.DTOs
{
    // ===================================
    // GENERIC WRAPPERS (keep these as-is)
    // ===================================
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public int StatusCode { get; set; }
        public string? ErrorCode { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResponse(T data, string? message = "Success")
            => Ok(data, message);

        public static ApiResponse<T> ErrorResponse(string errorCode, string message, List<string>? errors = null)
            => new()
            {
                Success = false,
                StatusCode = 400,
                ErrorCode = errorCode,
                Message = message,
                Errors = errors
            };

        // =========================
        // SUCCESS RESPONSES
        // =========================

        public static ApiResponse<T> Ok(T data, string? message = "Success")
            => new()
            {
                Success = true,
                StatusCode = 200,
                Data = data,
                Message = message
            };

        public static ApiResponse<T> Created(T data, string? message = "Data created successfully")
            => new()
            {
                Success = true,
                StatusCode = 201,
                Data = data,
                Message = message
            };

        public static ApiResponse<T> NoContent(string? message = "No content")
            => new()
            {
                Success = true,
                StatusCode = 204,
                Message = message
            };

        // =========================
        // CLIENT ERRORS
        // =========================

        public static ApiResponse<T> BadRequest(string message = "Bad request", List<string>? errors = null)
            => new()
            {
                Success = false,
                StatusCode = 400,
                ErrorCode = "BAD_REQUEST",
                Message = message,
                Errors = errors
            };

        public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
            => new()
            {
                Success = false,
                StatusCode = 401,
                ErrorCode = "UNAUTHORIZED",
                Message = message
            };

        public static ApiResponse<T> Forbidden(string message = "Forbidden")
            => new()
            {
                Success = false,
                StatusCode = 403,
                ErrorCode = "FORBIDDEN",
                Message = message
            };

        public static ApiResponse<T> NotFound(string message = "Data not found")
            => new()
            {
                Success = false,
                StatusCode = 404,
                ErrorCode = "NOT_FOUND",
                Message = message
            };

        public static ApiResponse<T> Conflict(string message = "Data already exists")
            => new()
            {
                Success = false,
                StatusCode = 409,
                ErrorCode = "CONFLICT",
                Message = message
            };

        public static ApiResponse<T> UnprocessableEntity(string message = "Validation failed", List<string>? errors = null)
            => new()
            {
                Success = false,
                StatusCode = 422,
                ErrorCode = "UNPROCESSABLE_ENTITY",
                Message = message,
                Errors = errors
            };

        // =========================
        // SERVER ERRORS
        // =========================

        public static ApiResponse<T> InternalServerError(string message = "Internal server error")
            => new()
            {
                Success = false,
                StatusCode = 500,
                ErrorCode = "INTERNAL_SERVER_ERROR",
                Message = message
            };
    }

    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    // ===================================
    // EXAMPLE: PRODUCT DTOs
    // Copy this pattern for each new resource.
    // Rename "Product" to your entity name.
    // ===================================

    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; }
    }

    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public string? OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class ProductFilterRequest
    {
        public string? Category { get; set; }
        public bool? IsActive { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
