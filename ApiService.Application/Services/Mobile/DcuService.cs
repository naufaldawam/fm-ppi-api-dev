
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ApiService.Application.Configurations;
using ApiService.Application.DTOs;
using ApiService.Application.DTOs.Mobile;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services.Mobile
{
    public interface IDcuService
    {
        Task<ApiResponse<PagedResponse<DataDcuDto>>> GetAllAsync(DataDcuFilterRequest filter);
        Task<ApiResponse<DataDcuDto>> GetByIdAsync(string id);
        Task<ApiResponse<DataDcuDto>> CreateAsync(CreateDcuRequest request, IFormFile photo, string userId);
        Task<ApiResponse<DataDcuDto>> UpdateAsync(string id, UpdateDcuRequest request, IFormFile? photo, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        Task<ApiResponse<EvidenceDcuDto>> UploadEvidenceAsync(string dataDcuId, IFormFile photo, string userId);
        Task<ApiResponse<bool>> DeleteEvidenceAsync(string evidenceId, string userId);
        Task<ApiResponse<EvidenceFileDto>> GetEvidenceImageAsync(string evidenceId);
    }

    public class DcuService : IDcuService
    {
        private readonly IServiceDbContext _context;
        private readonly IFileService _fileService;
        private readonly StorageConfig _storage;

        public DcuService(
            IServiceDbContext context,
            IFileService fileService,
            IOptions<StorageConfig> storage)
        {
            _context = context;
            _fileService = fileService;
            _storage = storage.Value;
        }

        private IQueryable<DailyCheckUpEntity> BaseQuery() =>
            _context.DailyCheckUpEntities
                .Include(x => x.Driver)
                .Include(x => x.Evidences)
                .Where(x => !x.IsDeleted);

        public async Task<ApiResponse<PagedResponse<DataDcuDto>>> GetAllAsync(DataDcuFilterRequest filter)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 10 : Math.Min(filter.PageSize, 100);

            var query = BaseQuery();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();
                query = query.Where(x =>
                    (x.Driver != null && x.Driver.NoPekerja.Contains(search)) ||
                    (x.Driver != null && x.Driver.NamaDriver.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filter.DriverId))
                query = query.Where(x => x.DriverId == filter.DriverId);

            if (!string.IsNullOrWhiteSpace(filter.StatusKesehatan))
                query = query.Where(x => x.StatusKesehatan == filter.StatusKesehatan);

            if (filter.TanggalFrom.HasValue)
            {
                var from = filter.TanggalFrom.Value.Date;
                query = query.Where(x => x.TanggalDcu >= from);
            }

            if (filter.TanggalTo.HasValue)
            {
                var to = filter.TanggalTo.Value.Date.AddDays(1);
                query = query.Where(x => x.TanggalDcu < to);
            }

            if (filter.IsActive.HasValue)
                query = query.Where(x => x.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.TanggalDcu)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<DataDcuDto>>.SuccessResponse(
                new PagedResponse<DataDcuDto>
                {
                    Items = items.Select(MapToDto).ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                });
        }

        public async Task<ApiResponse<DataDcuDto>> GetByIdAsync(string id)
        {
            var item = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);

            if (item == null)
                return ApiResponse<DataDcuDto>.NotFound("Data DCU tidak ditemukan.");

            return ApiResponse<DataDcuDto>.SuccessResponse(MapToDto(item));
        }

        public async Task<ApiResponse<DataDcuDto>> CreateAsync(
            CreateDcuRequest request,
            IFormFile photo,
            string userId)
        {
            if (string.IsNullOrWhiteSpace(request.DriverId))
                return ApiResponse<DataDcuDto>.BadRequest("Driver wajib dipilih.");

            if (photo == null || photo.Length == 0)
                return ApiResponse<DataDcuDto>.BadRequest("Foto dokumentasi wajib diupload.");

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(x => x.Id == request.DriverId && !x.IsDeleted && x.IsActive);

            if (driver == null)
                return ApiResponse<DataDcuDto>.ErrorResponse("ERR-DCU-001", "Driver tidak ditemukan atau tidak aktif.");

            var statusValidation = ValidateStatus(request.StatusKesehatan);
            if (statusValidation != null)
                return ApiResponse<DataDcuDto>.BadRequest(statusValidation);

            var duplicate = await _context.DailyCheckUpEntities.AnyAsync(x =>
                !x.IsDeleted &&
                x.DriverId == request.DriverId &&
                x.TanggalDcu.Date == request.TanggalDcu.Date);

            if (duplicate)
                return ApiResponse<DataDcuDto>.ErrorResponse(
                    "ERR-DCU-002",
                    "DCU untuk driver pada tanggal tersebut sudah terdaftar.");

            var dcu = new DailyCheckUpEntity
            {
                DriverId = request.DriverId,
                TanggalDcu = request.TanggalDcu.Date,
                TekananDarahSistolik = request.TekananDarahSistolik,
                TekananDarahDiastolik = request.TekananDarahDiastolik,
                SaturasiOksigen = request.SaturasiOksigen,
                NadiDenyut = request.NadiDenyut,
                SuhuTubuh = request.SuhuTubuh,
                StatusKesehatan = NormalizeStatus(request.StatusKesehatan)!,
                Keterangan = request.Keterangan,
                IsActive = true,
                CreatedBy = userId
            };

            var uploadResult = await _fileService.UploadImageAsync(photo, userId);
            if (!uploadResult.Success)
                return ApiResponse<DataDcuDto>.ErrorResponse(uploadResult.ErrorCode!, uploadResult.Message!);

            _context.DailyCheckUpEntities.Add(dcu);

            var evidence = new DailyCheckUpEvidance
            {
                DataDcuId = dcu.Id,
                FileName = uploadResult.Data!.FileName,
                GeneratedName = uploadResult.Data.GeneratedName,
                FilePath = BuildFilePath(uploadResult.Data.GeneratedName),
                FileSize = uploadResult.Data.FileSize,
                ContentType = uploadResult.Data.ContentType,
                SortOrder = 1,
                IsActive = true,
                CreatedBy = userId
            };

            _context.DailyCheckUpEvidances.Add(evidence);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(x => x.Id == dcu.Id);
            return ApiResponse<DataDcuDto>.SuccessResponse(
                MapToDto(created),
                "DCU berhasil disimpan.");
        }

        public async Task<ApiResponse<DataDcuDto>> UpdateAsync(
            string id,
            UpdateDcuRequest request,
            IFormFile? photo,
            string userId)
        {
            var dcu = await _context.DailyCheckUpEntities
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (dcu == null)
                return ApiResponse<DataDcuDto>.NotFound("Data DCU tidak ditemukan.");

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(x => x.Id == request.DriverId && !x.IsDeleted && x.IsActive);

            if (driver == null)
                return ApiResponse<DataDcuDto>.ErrorResponse("ERR-DCU-001", "Driver tidak ditemukan atau tidak aktif.");

            var statusValidation = ValidateStatus(request.StatusKesehatan);
            if (statusValidation != null)
                return ApiResponse<DataDcuDto>.BadRequest(statusValidation);

            var duplicate = await _context.DailyCheckUpEntities.AnyAsync(x =>
                !x.IsDeleted &&
                x.Id != id &&
                x.DriverId == request.DriverId &&
                x.TanggalDcu.Date == request.TanggalDcu.Date);

            if (duplicate)
                return ApiResponse<DataDcuDto>.ErrorResponse(
                    "ERR-DCU-002",
                    "DCU untuk driver pada tanggal tersebut sudah terdaftar.");

            dcu.DriverId = request.DriverId;
            dcu.TanggalDcu = request.TanggalDcu.Date;
            dcu.TekananDarahSistolik = request.TekananDarahSistolik;
            dcu.TekananDarahDiastolik = request.TekananDarahDiastolik;
            dcu.SaturasiOksigen = request.SaturasiOksigen;
            dcu.NadiDenyut = request.NadiDenyut;
            dcu.SuhuTubuh = request.SuhuTubuh;
            dcu.StatusKesehatan = NormalizeStatus(request.StatusKesehatan)!;
            dcu.Keterangan = request.Keterangan;
            dcu.IsActive = request.IsActive;
            dcu.ModifiedAt = DateTime.UtcNow;
            dcu.ModifiedBy = userId;

            if (photo != null && photo.Length > 0)
            {
                var uploadResult = await _fileService.UploadImageAsync(photo, userId);
                if (!uploadResult.Success)
                    return ApiResponse<DataDcuDto>.ErrorResponse(uploadResult.ErrorCode!, uploadResult.Message!);

                var oldEvidence = await _context.DailyCheckUpEvidances
                    .Where(x => !x.IsDeleted && x.DataDcuId == id)
                    .OrderByDescending(x => x.SortOrder)
                    .FirstOrDefaultAsync();

                if (oldEvidence != null)
                {
                    oldEvidence.IsDeleted = true;
                    oldEvidence.DeletedAt = DateTime.UtcNow;
                    oldEvidence.DeletedBy = userId;
                }

                var evidence = new DailyCheckUpEvidance
                {
                    DataDcuId = dcu.Id,
                    FileName = uploadResult.Data!.FileName,
                    GeneratedName = uploadResult.Data.GeneratedName,
                    FilePath = BuildFilePath(uploadResult.Data.GeneratedName),
                    FileSize = uploadResult.Data.FileSize,
                    ContentType = uploadResult.Data.ContentType,
                    SortOrder = (oldEvidence?.SortOrder ?? 0) + 1,
                    IsActive = true,
                    CreatedBy = userId
                };

                _context.DailyCheckUpEvidances.Add(evidence);
            }

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(x => x.Id == dcu.Id);
            return ApiResponse<DataDcuDto>.SuccessResponse(
                MapToDto(updated),
                "DCU berhasil diperbarui.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var dcu = await _context.DailyCheckUpEntities
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (dcu == null)
                return ApiResponse<bool>.NotFound("Data DCU tidak ditemukan.");

            dcu.IsDeleted = true;
            dcu.DeletedAt = DateTime.UtcNow;
            dcu.DeletedBy = userId;

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Data DCU berhasil dihapus.");
        }

        public async Task<ApiResponse<EvidenceDcuDto>> UploadEvidenceAsync(
            string dataDcuId,
            IFormFile photo,
            string userId)
        {
            var exists = await _context.DailyCheckUpEntities
                .AnyAsync(x => x.Id == dataDcuId && !x.IsDeleted);

            if (!exists)
                return ApiResponse<EvidenceDcuDto>.NotFound("Data DCU tidak ditemukan.");

            var uploadResult = await _fileService.UploadImageAsync(photo, userId);
            if (!uploadResult.Success)
                return ApiResponse<EvidenceDcuDto>.ErrorResponse(uploadResult.ErrorCode!, uploadResult.Message!);

            var maxSort = await _context.DailyCheckUpEvidances
                .Where(x => !x.IsDeleted && x.DataDcuId == dataDcuId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? 0;

            var evidence = new DailyCheckUpEvidance
            {
                DataDcuId = dataDcuId,
                FileName = uploadResult.Data!.FileName,
                GeneratedName = uploadResult.Data.GeneratedName,
                FilePath = BuildFilePath(uploadResult.Data.GeneratedName),
                FileSize = uploadResult.Data.FileSize,
                ContentType = uploadResult.Data.ContentType,
                SortOrder = maxSort + 1,
                IsActive = true,
                CreatedBy = userId
            };

            _context.DailyCheckUpEvidances.Add(evidence);
            await _context.SaveChangesAsync();

            return ApiResponse<EvidenceDcuDto>.SuccessResponse(
                MapEvidenceToDto(evidence),
                "Foto dokumentasi berhasil ditambahkan.");
        }

        public async Task<ApiResponse<bool>> DeleteEvidenceAsync(string evidenceId, string userId)
        {
            var evidence = await _context.DailyCheckUpEvidances
                .FirstOrDefaultAsync(x => x.Id == evidenceId && !x.IsDeleted);

            if (evidence == null)
                return ApiResponse<bool>.NotFound("Foto dokumentasi tidak ditemukan.");

            evidence.IsDeleted = true;
            evidence.DeletedAt = DateTime.UtcNow;
            evidence.DeletedBy = userId;

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Foto dokumentasi berhasil dihapus.");
        }

        public async Task<ApiResponse<EvidenceFileDto>> GetEvidenceImageAsync(string evidenceId)
        {
            var evidence = await _context.DailyCheckUpEvidances
                .FirstOrDefaultAsync(x => x.Id == evidenceId && !x.IsDeleted);

            if (evidence == null)
                return ApiResponse<EvidenceFileDto>.ErrorResponse(
                    "ERR-EVIDENCE-DCU-001", "Foto dokumentasi tidak ditemukan.");

            if (!File.Exists(evidence.FilePath))
                return ApiResponse<EvidenceFileDto>.NotFound("File foto dokumentasi tidak ditemukan.");

            var fileBytes = await File.ReadAllBytesAsync(evidence.FilePath);

            return ApiResponse<EvidenceFileDto>.SuccessResponse(new EvidenceFileDto
            {
                FileName = evidence.FileName,
                ContentType = string.IsNullOrWhiteSpace(evidence.ContentType)
                    ? "image/jpeg"
                    : evidence.ContentType,
                Bytes = fileBytes
            });
        }

        private string? ValidateStatus(string? status)
        {
            var normalized = NormalizeStatus(status);
            return normalized == null
                ? "Status kesehatan hanya boleh Fit atau Unfit."
                : null;
        }

        private string? NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            if (string.Equals(status.Trim(), DailyCheckUpEntity.StatusFit, StringComparison.OrdinalIgnoreCase))
                return DailyCheckUpEntity.StatusFit;

            if (string.Equals(status.Trim(), DailyCheckUpEntity.StatusUnfit, StringComparison.OrdinalIgnoreCase))
                return DailyCheckUpEntity.StatusUnfit;

            return null;
        }

        private string BuildFilePath(string generatedName) =>
            Path.Combine(
                _storage.UploadPath ?? string.Empty,
                _storage.ImageFolder ?? string.Empty,
                generatedName);

        private string BuildEvidenceUrl(string generatedName) =>
            $"{_storage.BaseUrl}{_storage.ImageFolder}/{generatedName}";

        private EvidenceDcuDto MapEvidenceToDto(DailyCheckUpEvidance evidence) => new()
        {
            Id = evidence.Id,
            DataDcuId = evidence.DataDcuId,
            FileName = evidence.FileName,
            GeneratedName = evidence.GeneratedName,
            FileSize = evidence.FileSize,
            ContentType = evidence.ContentType,
            Url = BuildEvidenceUrl(evidence.GeneratedName)
        };

        private DataDcuDto MapToDto(DailyCheckUpEntity item) => new()
        {
            Id = item.Id,
            DriverId = item.DriverId,
            NoPekerjaDriver = item.Driver?.NoPekerja ?? string.Empty,
            NamaDriver = item.Driver?.NamaDriver ?? string.Empty,
            TanggalDcu = item.TanggalDcu,
            TekananDarahSistolik = item.TekananDarahSistolik,
            TekananDarahDiastolik = item.TekananDarahDiastolik,
            SaturasiOksigen = item.SaturasiOksigen,
            NadiDenyut = item.NadiDenyut,
            SuhuTubuh = item.SuhuTubuh,
            StatusKesehatan = item.StatusKesehatan,
            Keterangan = item.Keterangan,
            IsActive = item.IsActive,
            Evidence = item.Evidences
                .Where(e => !e.IsDeleted)
                .OrderByDescending(e => e.SortOrder)
                .Select(MapEvidenceToDto)
                .FirstOrDefault(),
            CreatedAt = item.CreatedAt,
            ModifiedAt = item.ModifiedAt
        };
    }
}
