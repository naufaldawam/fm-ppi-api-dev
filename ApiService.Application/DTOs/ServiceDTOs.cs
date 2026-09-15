using System;
using System.Collections.Generic;
using System.IO;

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
        public bool IsActive { get; set; } = true;
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
        public string JabatanId { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;
        /// <summary>
        /// Kode RF.ID yang di-assign ke pekerja ini - supaya form Member Parkir
        /// langsung terisi field Rfid saat user pilih Pekerja.
        /// </summary>
        public List<string> RfIds { get; set; } = new();
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
        public string KepemilikanId { get; set; } = string.Empty;
        public string JabatanId { get; set; } = string.Empty;
        public string? PekerjaId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateKendaraanRequest
    {
        public string NomorPolisi { get; set; } = string.Empty;
        public string TipeId { get; set; } = string.Empty;
        public string BahanBakarId { get; set; } = string.Empty;
        public string Merek { get; set; } = string.Empty;
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
        public string? PekerjaId { get; set; }
        public string KendaraanId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateRfIdRequest
    {
        public string RfIdCode { get; set; } = string.Empty;
        public string? PekerjaId { get; set; }
        public string KendaraanId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class RfIdDto
    {
        public string Id { get; set; } = string.Empty;
        public string RfIdCode { get; set; } = string.Empty;
        public string? PekerjaId { get; set; }
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
    // RF.ID - BULK UPLOAD
    // Kolom template: RF.ID | No.Pekerja | Nopol
    // (pakai No.Pekerja & Nopol sebagai key lookup karena keduanya unique,
    // beda dengan Nama Pekerja yang bisa duplikat)
    // ===================================
    public class RfIdImportRow
    {
        public int RowNumber { get; set; }
        public string RfIdCode { get; set; } = string.Empty;
        public string NoPekerja { get; set; } = string.Empty;
        public string Nopol { get; set; } = string.Empty;
    }

    public class RfIdImportPreviewDto
    {
        public string RfIdCode { get; set; } = string.Empty;
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
        public string Nopol { get; set; } = string.Empty;
    }

    public class RfIdImportResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int InsertedRfId { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();
        public List<RfIdImportPreviewDto> Preview { get; set; } = new();
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
        public string? AtasanId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateDriverRequest
    {
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public string NoHp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string? AtasanId { get; set; }
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
        public string? AtasanId { get; set; }
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

    // ===================================
    // KENDARAAN - BULK UPLOAD
    // Kolom template: Nopol | Merek | Tipe | BahanBakar | Vendor | Kepemilikan | Jabatan
    // (pakai Name sebagai key lookup, karena Name bersifat unique per master)
    // ===================================

    public class KendaraanImportRow
    {
        public int RowNumber { get; set; }
        public string NomorPolisi { get; set; } = string.Empty;
        public string Merek { get; set; } = string.Empty;
        public string TipeName { get; set; } = string.Empty;
        public string BahanBakarName { get; set; } = string.Empty;
        public string KepemilikanName { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;
        public string NoPekerja { get; set; } = string.Empty;
    }

    public class KendaraanImportPreviewDto
    {
        public string NomorPolisi { get; set; } = string.Empty;
        public string Merek { get; set; } = string.Empty;
        public string TipeName { get; set; } = string.Empty;
        public string BahanBakarName { get; set; } = string.Empty;
        public string KepemilikanName { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;
        public string NamaPejabat { get; set; } = string.Empty;
    }

    public class KendaraanImportResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int InsertedKendaraan { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();
        public List<KendaraanImportPreviewDto> Preview { get; set; } = new();
    }

    // ===================================
    // SHARED: BULK UPLOAD (dipakai semua modul: Pekerja, RF.ID, Driver, Kendaraan, dst.)
    // ===================================

    /// <summary>Hasil file untuk endpoint download (template, export, dsb).</summary>
    public class FileResult
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public Stream FileStream { get; set; } = null!;
    }

    /// <summary>Satu baris error validasi hasil bulk upload.</summary>
    public class ImportRowError
    {
        public int RowNumber { get; set; }
        public string Column { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // ===================================
    // PEKERJA - BULK UPLOAD
    // ===================================
    public class PekerjaImportRow
    {
        public int RowNumber { get; set; }
        public string NoPekerja { get; set; } = string.Empty;
        public string NopekHome { get; set; } = string.Empty;
        public string NopekHost { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;
    }

    public class PekerjaImportPreviewDto
    {
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;
    }

    public class PekerjaImportResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int InsertedPekerja { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();
        public List<PekerjaImportPreviewDto> Preview { get; set; } = new();
    }

    // ===================================
    // DRIVER - BULK UPLOAD
    // Kolom template: NoPekerja | NamaDriver | NoHp | Email | Vendor | NoPekerjaAtasan
    // Lookup Vendor via Name; lookup Atasan via NoPekerja (unique di master Pekerja)
    // ===================================

    public class DriverImportRow
    {
        public int RowNumber { get; set; }
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public string NoHp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string NoPekerjaAtasan { get; set; } = string.Empty;
    }

    public class DriverImportPreviewDto
    {
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public string NoHp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string NamaPekerjaAtasan { get; set; } = string.Empty;
    }

    public class DriverImportResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int InsertedDriver { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();
        public List<DriverImportPreviewDto> Preview { get; set; } = new();
    }

    // ===================================
    // PERIODE (Data Master > Periode)
    // ===================================
    public class CreatePeriodeRequest
    {
        public string NamaPeriode { get; set; } = string.Empty;
        public DateTime TanggalAwal { get; set; }
        public DateTime TanggalAkhir { get; set; }
        public bool IsActive { get; set; } = false;
    }

    public class UpdatePeriodeRequest
    {
        public string NamaPeriode { get; set; } = string.Empty;
        public DateTime TanggalAwal { get; set; }
        public DateTime TanggalAkhir { get; set; }
        public bool IsActive { get; set; } = false;
    }

    public class PeriodeDto
    {
        public string Id { get; set; } = string.Empty;
        public string NamaPeriode { get; set; } = string.Empty;
        public DateTime TanggalAwal { get; set; }
        public DateTime TanggalAkhir { get; set; }
        public bool IsActive { get; set; }
        /// <summary>
        /// True hanya jika IsActive == true DAN tanggal sekarang berada di antara
        /// TanggalAwal - TanggalAkhir (inklusif). Salah satu saja tidak terpenuhi -> false.
        /// </summary>
        public bool IsCurrentlyActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class PeriodeFilterRequest
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PeriodeLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string NamaPeriode { get; set; } = string.Empty;
        public DateTime TanggalAwal { get; set; }
        public DateTime TanggalAkhir { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrentlyActive { get; set; }
    }

    public class PeriodeStatusDto
    {
        public string Id { get; set; } = string.Empty;
        public string NamaPeriode { get; set; } = string.Empty;
        public DateTime TanggalAwal { get; set; }
        public DateTime TanggalAkhir { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrentlyActive { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>DTO ringan untuk dropdown/lookup master data sederhana (Id + Name).
    /// Dipakai bareng oleh Jabatan, Vendor, Tipe, BahanBakar, Kepemilikan.</summary>
    public class MasterLookupDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // ===================================
    // MEMBER PARKIR (Data Master > Member Parkir)
    // Field: NamaPekerja, Rfid, No.Pekerja, Jabatan, Periode, Tanggal Penagihan, Jumlah Biaya
    // NamaPekerja / No.Pekerja / Jabatan / RF.ID otomatis diisi dari Pekerja (server-side)
    // ===================================
    public class CreateMemberParkirRequest
    {
        public string PekerjaId { get; set; } = string.Empty;
        /// <summary>Periode di mana record ini dibuat (dropdown dari master Periode).</summary>
        public string PeriodeId { get; set; } = string.Empty;
        public DateTime TanggalPenagihan { get; set; }
        public decimal JumlahBiaya { get; set; }
    }

    public class UpdateMemberParkirRequest
    {
        public string PekerjaId { get; set; } = string.Empty;
        /// <summary>Periode di mana record ini dibuat (dropdown dari master Periode).</summary>
        public string PeriodeId { get; set; } = string.Empty;
        public DateTime TanggalPenagihan { get; set; }
        public decimal JumlahBiaya { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class MemberParkirDto
    {
        public string Id { get; set; } = string.Empty;
        public string PekerjaId { get; set; } = string.Empty;
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
        public string JabatanId { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;

        /// <summary>
        /// Semua kode RF.ID milik Pekerja ini (bisa kosong kalau belum punya RF.ID
        /// sama sekali, atau berisi lebih dari 1 kalau punya beberapa kartu).
        /// Ditarik langsung dari Pekerja.RfIds, bukan disimpan di MemberParkir.
        /// </summary>
        public List<string> RfIds { get; set; } = new();

        public string PeriodeId { get; set; } = string.Empty;
        public string NamaPeriode { get; set; } = string.Empty;
        public DateTime TanggalPenagihan { get; set; }
        public decimal JumlahBiaya { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class MemberParkirFilterRequest
    {
        /// <summary>Cari berdasarkan No.Pekerja atau Nama Pekerja.</summary>
        public string? Search { get; set; }
        public string? JabatanId { get; set; }
        public string? PeriodeId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ===================================
    // MEMBER PARKIR - BULK UPLOAD
    // Kolom template: NoPekerja | Periode | TanggalPenagihan | JumlahBiaya
    // Lookup Pekerja via NoPekerja; Periode via NamaPeriode.
    // RF.ID TIDAK ada di template - selalu ditarik dari Pekerja.RfIds saat ditampilkan.
    // ===================================
    public class MemberParkirImportRow
    {
        public int RowNumber { get; set; }
        public string NoPekerja { get; set; } = string.Empty;
        public string PeriodeName { get; set; } = string.Empty;
        public string TanggalPenagihanText { get; set; } = string.Empty;
        public DateTime? TanggalPenagihan { get; set; }
        public string JumlahBiayaText { get; set; } = string.Empty;
        public decimal? JumlahBiaya { get; set; }
    }

    public class MemberParkirImportPreviewDto
    {
        public string NoPekerja { get; set; } = string.Empty;
        public string NamaPekerja { get; set; } = string.Empty;
        public string JabatanName { get; set; } = string.Empty;
        public List<string> RfIds { get; set; } = new();
        public string NamaPeriode { get; set; } = string.Empty;
        public DateTime TanggalPenagihan { get; set; }
        public decimal JumlahBiaya { get; set; }
    }

    public class MemberParkirImportResponse
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int InsertedMemberParkir { get; set; }
        public List<ImportRowError> Errors { get; set; } = new();
        public List<MemberParkirImportPreviewDto> Preview { get; set; } = new();
    }

    // ===================================
    // MEMBER PARKIR - SUMMARY (Total & Grand Total)
    // ===================================
    public class MemberParkirSummaryRequest
    {
        /// <summary>Opsional - kalau diisi, summary dihitung hanya untuk periode ini.</summary>
        public string? PeriodeId { get; set; }
        /// <summary>Opsional - kalau diisi, summary difilter berdasarkan status aktif.</summary>
        public bool? IsActive { get; set; }
    }

    public class MemberParkirSummaryDto
    {
        /// <summary>Filter periode yang dipakai untuk menghitung summary ini (null = semua periode).</summary>
        public string? PeriodeId { get; set; }
        public string? NamaPeriode { get; set; }

        /// <summary>Jumlah record Member Parkir yang match filter.</summary>
        public int TotalMemberParkir { get; set; }

        /// <summary>Total keseluruhan JumlahBiaya dari semua record yang match filter.</summary>
        public decimal GrandTotalBiaya { get; set; }
    }
}