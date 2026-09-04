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

    public class GetDataUsers
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool? IsUserGiveFeedback { get; set; }
        public bool? ShowFeedback { get; set; }
        public int? TotalUserHasFeedback { get; set; }
    }

    public class GetDataUserRolesApprover
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

    }
    
    public class CreateFeedbackRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class UpdateFeedbackStatusRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class FeedbackSummaryResponse
    {
        public int TotalFeedback { get; set; }
        public decimal AverageRating { get; set; }
        public int RatingTrendThisMonth { get; set; }
        public int RatingTrendLastMonth { get; set; }
    }

    public class FeedbackFilterRequest
    {
        public int? Rating { get; set; }
        public bool? WithComment { get; set; }

        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }

    public class FeedbackResponse
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    // ===================================
    // JABATAN (Master Data)
    // ===================================
    public class CreateJabatanRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateJabatanRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class JabatanDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class JabatanFilterRequest
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ===================================
    // TIPE (Master Data)
    // ===================================
    public class CreateTipeRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateTipeRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class TipeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class TipeFilterRequest
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ===================================
    // BAHAN BAKAR (Master Data)
    // ===================================
    public class CreateBahanBakarRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateBahanBakarRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class BahanBakarDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class BahanBakarFilterRequest
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ===================================
    // KEPEMILIKAN (Master Data)
    // ===================================
    public class CreateKepemilikanRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateKepemilikanRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class KepemilikanDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class KepemilikanFilterRequest
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ===================================
    // VENDOR (Master Data)
    // ===================================
    public class CreateVendorRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateVendorRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class VendorDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class VendorFilterRequest
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ===================================
    // PEKERJA (Data Master > Pekerja) - master independen
    // ===================================
    public class CreatePekerjaRequest
    {
        public string NoPekerja { get; set; } = string.Empty;
        public string NopekHome { get; set; } = string.Empty;
        public string NopekHost { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
        public string JabatanId { get; set; } = string.Empty;
    }

    public class UpdatePekerjaRequest
    {
        public string NoPekerja { get; set; } = string.Empty;
        public string NopekHome { get; set; } = string.Empty;
        public string NopekHost { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
        public string JabatanId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class PekerjaDto
    {
        public string Id { get; set; } = string.Empty;
        public string NoPekerja { get; set; } = string.Empty;
        public string NopekHome { get; set; } = string.Empty;
        public string NopekHost { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
        public string JabatanId { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;
        // Read-only: di-assign lewat menu RF.ID terpisah, bisa kosong kalau belum di-assign
        public List<string> RfIds { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class PekerjaFilterRequest
    {
        public string? Search { get; set; }
        public string? JabatanId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>DTO ringan untuk dropdown/lookup Pekerja (tanpa pagination).</summary>
    public class PekerjaLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
    }

        // ===================================
    // KENDARAAN (Data Master > Kendaraan)
    // Referensi: Tipe, BahanBakar, Vendor, Kepemilikan, Jabatan (alokasi jabatan),
    // Pekerja (pejabat pemegang - opsional)
    // ===================================
    public class CreateKendaraanRequest
    {
        public string NomorPolisi { get; set; } = string.Empty;
        public string TipeId { get; set; } = string.Empty;
        public string BahanBakarId { get; set; } = string.Empty;
        public string Merek { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string KepemilikanId { get; set; } = string.Empty;
        public string JabatanId { get; set; } = string.Empty;
        public string? PekerjaId { get; set; }
    }

    public class UpdateKendaraanRequest
    {
        public string NomorPolisi { get; set; } = string.Empty;
        public string TipeId { get; set; } = string.Empty;
        public string BahanBakarId { get; set; } = string.Empty;
        public string Merek { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string KepemilikanId { get; set; } = string.Empty;
        public string JabatanId { get; set; } = string.Empty;
        public string? PekerjaId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class KendaraanDto
    {
        public string Id { get; set; } = string.Empty;
        public string NomorPolisi { get; set; } = string.Empty;

        public string TipeId { get; set; } = string.Empty;
        public string TipeName { get; set; } = string.Empty;

        public string BahanBakarId { get; set; } = string.Empty;
        public string BahanBakarName { get; set; } = string.Empty;

        public string Merek { get; set; } = string.Empty;

        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;

        public string KepemilikanId { get; set; } = string.Empty;
        public string KepemilikanName { get; set; } = string.Empty;

        public string JabatanId { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;

        public string? PekerjaId { get; set; }
        public string? PekerjaName { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>DTO ringan untuk dropdown/lookup Kendaraan (tanpa pagination).</summary>
    public class KendaraanLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string NomorPolisi { get; set; } = string.Empty;
    }

    public class KendaraanFilterRequest
    {
        /// <summary>Cari berdasarkan Nomor Polisi atau Merek Kendaraan.</summary>
        public string? Search { get; set; }
        public string? TipeId { get; set; }
        public string? BahanBakarId { get; set; }
        public string? VendorId { get; set; }
        public string? KepemilikanId { get; set; }
        public string? JabatanId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ===================================
    // RF.ID (Data Master > RF.ID)
    // Referensi: Pekerja (pemegang), Kendaraan (Nopol)
    // ===================================
    public class CreateRfIdRequest
    {
        public string RfIdCode { get; set; } = string.Empty;
        public string PekerjaId { get; set; } = string.Empty;
        public string KendaraanId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateRfIdRequest
    {
        public string RfIdCode { get; set; } = string.Empty;
        public string PekerjaId { get; set; } = string.Empty;
        public string KendaraanId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class RfIdDto
    {
        public string Id { get; set; } = string.Empty;
        public string RfIdCode { get; set; } = string.Empty;

        public string PekerjaId { get; set; } = string.Empty;
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;

        public string KendaraanId { get; set; } = string.Empty;
        public string NomorPolisi { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class RfIdFilterRequest
    {
        /// <summary>Cari berdasarkan RF.ID, No. Pekerja, Nama Pekerja, atau Nomor Polisi.</summary>
        public string? Search { get; set; }
        public string? PekerjaId { get; set; }
        public string? KendaraanId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ===================================
    // DRIVER (Data Master > Driver)
    // Referensi: Vendor, Atasan (Pekerja)
    // ===================================
    public class CreateDriverRequest
    {
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public string NoHp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string AtasanId { get; set; } = string.Empty;
    }

    public class UpdateDriverRequest
    {
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public string NoHp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string AtasanId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class DriverDto
    {
        public string Id { get; set; } = string.Empty;
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public string NoHp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;

        public string AtasanId { get; set; } = string.Empty;
        public string AtasanNama { get; set; } = string.Empty;
        public string JabatanAtasan { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class DriverFilterRequest
    {
        /// <summary>Cari berdasarkan No.Pekerja, Nama Driver, No.HP, atau Email.</summary>
        public string? Search { get; set; }
        public string? VendorId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>DTO ringan untuk dropdown/lookup master data sederhana (Id + Name).
    /// Dipakai bareng oleh Jabatan, Vendor, Tipe, BahanBakar, Kepemilikan.</summary>
    public class MasterLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
