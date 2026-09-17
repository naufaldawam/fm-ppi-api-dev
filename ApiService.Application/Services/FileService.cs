using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using ApiService.Application.Configurations;
using ApiService.Application.DTOs;

namespace ApiService.Application.Services
{
    public interface IFileService
    {
        Task<ApiResponse<FileUploadResponse>> UploadImageAsync(IFormFile file, string uploadedBy);
        Task<ApiResponse<FileUploadResponse>> UploadAttachmentAsync(IFormFile file, string uploadedBy);
    }

    public class FileService : IFileService
    {
        private static readonly string[] AllowedImageContentTypes =
        {
            "image/jpeg", "image/png"
        };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const long MaxAttachmentSizeBytes = 20 * 1024 * 1024; // 20 MB

        private readonly StorageConfig _storage;

        public FileService(IOptions<StorageConfig> storage)
        {
            _storage = storage.Value;
        }

        public async Task<ApiResponse<FileUploadResponse>> UploadImageAsync(IFormFile file, string uploadedBy)
            => await SaveFileAsync(file, _storage.ImageFolder, AllowedImageContentTypes, MaxImageSizeBytes, uploadedBy);

        public async Task<ApiResponse<FileUploadResponse>> UploadAttachmentAsync(IFormFile file, string uploadedBy)
            => await SaveFileAsync(file, _storage.AttachmentFolder, Array.Empty<string>(), MaxAttachmentSizeBytes, uploadedBy);

        private async Task<ApiResponse<FileUploadResponse>> SaveFileAsync(
            IFormFile file, string folder, string[] allowedTypes, long maxBytes, string uploadedBy)
        {
            if (file == null || file.Length == 0)
                return ApiResponse<FileUploadResponse>.ErrorResponse("ERR-FILE-001", "File tidak valid.");

            if (allowedTypes.Length > 0 && !Array.Exists(allowedTypes, t => t == file.ContentType.ToLowerInvariant()))
                return ApiResponse<FileUploadResponse>.ErrorResponse("ERR-FILE-002", "Format file tidak didukung.");

            if (file.Length > maxBytes)
                return ApiResponse<FileUploadResponse>.ErrorResponse("ERR-FILE-003", $"File melebihi batas ukuran {maxBytes / (1024 * 1024)} MB");

            var uploadPath = Path.Combine(_storage.UploadPath ?? string.Empty, folder ?? string.Empty);
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var cleanFileName = Path.GetFileName(file.FileName);
            var generatedName = $"{Guid.NewGuid()}_{cleanFileName}";
            var filePath = Path.Combine(uploadPath, generatedName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // BaseUrl diakhiri "/": https://storage-development.air.id/fm-images/xxx.jpg
            var baseUrl = _storage.BaseUrl ?? string.Empty;
            var url = $"{baseUrl}{folder}/{generatedName}";

            return ApiResponse<FileUploadResponse>.SuccessResponse(new FileUploadResponse
            {
                Id = generatedName,
                FileName = file.FileName,
                GeneratedName = generatedName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Url = generatedName == null ? string.Empty : url
            }, "File uploaded successfully.");
        }
    }
}