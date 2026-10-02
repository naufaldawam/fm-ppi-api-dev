using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.DTOs.Mobile;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities.Mobile;

namespace ApiService.Application.Services
{

    public interface IInspeksiKendaraanService
    {
        Task<ApiResponse<PagedResponse<InspeksiKendaraanDto>>> GetAllAsync(InspeksiKendaraanFilterRequest filter);
        Task<ApiResponse<InspeksiKendaraanDto>> GetByIdAsync(string id);
        Task<ApiResponse<InspeksiKendaraanDto>> CreateAsync(CreateInspeksiKendaraanRequest request, string userId);
        Task<ApiResponse<InspeksiKendaraanDto>> UpdateAsync(string id, UpdateInspeksiKendaraanRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }
    public class InspeksiKendaraanService(IServiceDbContext context) : IInspeksiKendaraanService
    {
        private readonly IServiceDbContext _context = context;

        private IQueryable<InspeksiKendaraanEntity> BaseQuery() =>
            _context.InspeksiKendaraanEntities
                .Include(x => x.Kendaraan)
                .Include(x => x.Driver)
                .Include(x => x.Details.Where(d => !d.IsDeleted).OrderBy(d => d.SortOrder))
                .Where(x => !x.IsDeleted);

        public async Task<ApiResponse<PagedResponse<InspeksiKendaraanDto>>> GetAllAsync(
            InspeksiKendaraanFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();
                query = query.Where(x =>
                    (x.Kendaraan != null && x.Kendaraan.NomorPolisi.Contains(search)) ||
                    (x.Driver != null && x.Driver.NoPekerja.Contains(search)) ||
                    (x.Driver != null && x.Driver.NamaDriver.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filter.KendaraanId))
                query = query.Where(x => x.KendaraanId == filter.KendaraanId);

            if (!string.IsNullOrWhiteSpace(filter.DriverId))
                query = query.Where(x => x.DriverId == filter.DriverId);

            if (filter.TanggalInspeksi.HasValue)
            {
                var date = filter.TanggalInspeksi.Value.Date;
                query = query.Where(x => x.TanggalInspeksi.Date == date);
            }

            if (!string.IsNullOrWhiteSpace(filter.KelayakanJalan))
                query = query.Where(x => x.KelayakanJalan == filter.KelayakanJalan);

            if (filter.IsActive.HasValue)
                query = query.Where(x => x.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.TanggalInspeksi)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<InspeksiKendaraanDto>>.SuccessResponse(
                new PagedResponse<InspeksiKendaraanDto>
                {
                    Items = items.Select(MapToDto).ToList(),
                    TotalCount = totalCount,
                    PageNumber = filter.Page,
                    PageSize = filter.PageSize
                });
        }

        public async Task<ApiResponse<InspeksiKendaraanDto>> GetByIdAsync(string id)
        {
            var entity = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return ApiResponse<InspeksiKendaraanDto>.ErrorResponse(
                    "ERR-INSPEKSIKENDARAAN-001", "Data inspeksi kendaraan tidak ditemukan.");

            return ApiResponse<InspeksiKendaraanDto>.SuccessResponse(MapToDto(entity));
        }

        public async Task<ApiResponse<InspeksiKendaraanDto>> CreateAsync(
            CreateInspeksiKendaraanRequest request,
            string userId)
        {
            var validation = await ValidateReferencesAsync(request.KendaraanId, request.DriverId);
            if (validation != null)
                return ApiResponse<InspeksiKendaraanDto>.ErrorResponse(validation.Value.Code, validation.Value.Message);

            if (request.Details == null || request.Details.Count == 0)
                return ApiResponse<InspeksiKendaraanDto>.BadRequest("Detail inspeksi wajib diisi.");

            var detailValidation = ValidateDetails(request.Details.Select(d => new DetailValidationItem(
                d.Kategori, d.NamaPemeriksaan, d.SortOrder)).ToList());
            if (detailValidation != null)
                return ApiResponse<InspeksiKendaraanDto>.BadRequest(detailValidation);

            var entity = new InspeksiKendaraanEntity
            {
                KendaraanId = request.KendaraanId,
                DriverId = request.DriverId,
                TanggalInspeksi = request.TanggalInspeksi,
                KelayakanJalan = CalculateKelayakan(request.Details.Select(x => x.IsOk)),
                Keterangan = request.Keterangan?.Trim(),
                IsActive = true,
                CreatedBy = userId
            };

            foreach (var item in request.Details.OrderBy(x => x.SortOrder))
            {
                entity.Details.Add(new InspeksiKendaraanDetailEntity
                {
                    Kategori = item.Kategori.Trim(),
                    NamaPemeriksaan = item.NamaPemeriksaan.Trim(),
                    IsOk = item.IsOk,
                    Keterangan = item.Keterangan?.Trim(),
                    SortOrder = item.SortOrder,
                    IsActive = true,
                    CreatedBy = userId
                });
            }

            _context.InspeksiKendaraanEntities.Add(entity);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(x => x.Id == entity.Id);
            return ApiResponse<InspeksiKendaraanDto>.SuccessResponse(
                MapToDto(created), "Inspeksi kendaraan berhasil ditambahkan.");
        }

        public async Task<ApiResponse<InspeksiKendaraanDto>> UpdateAsync(
            string id,
            UpdateInspeksiKendaraanRequest request,
            string userId)
        {
            var entity = await _context.InspeksiKendaraanEntities
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return ApiResponse<InspeksiKendaraanDto>.ErrorResponse(
                    "ERR-INSPEKSIKENDARAAN-001", "Data inspeksi kendaraan tidak ditemukan.");

            var validation = await ValidateReferencesAsync(request.KendaraanId, request.DriverId);
            if (validation != null)
                return ApiResponse<InspeksiKendaraanDto>.ErrorResponse(validation.Value.Code, validation.Value.Message);

            if (request.Details == null || request.Details.Count == 0)
                return ApiResponse<InspeksiKendaraanDto>.BadRequest("Detail inspeksi wajib diisi.");

            var detailValidation = ValidateDetails(request.Details.Select(d => new DetailValidationItem(
                d.Kategori, d.NamaPemeriksaan, d.SortOrder)).ToList());
            if (detailValidation != null)
                return ApiResponse<InspeksiKendaraanDto>.BadRequest(detailValidation);

            var now = DateTime.UtcNow;

            foreach (var oldDetail in entity.Details.Where(d => !d.IsDeleted))
            {
                oldDetail.IsDeleted = true;
                oldDetail.DeletedAt = now;
                oldDetail.DeletedBy = userId;
            }

            entity.KendaraanId = request.KendaraanId;
            entity.DriverId = request.DriverId;
            entity.TanggalInspeksi = request.TanggalInspeksi;
            entity.KelayakanJalan = CalculateKelayakan(request.Details.Select(x => x.IsOk));
            entity.Keterangan = request.Keterangan?.Trim();
            entity.ModifiedAt = now;
            entity.ModifiedBy = userId;

            foreach (var item in request.Details.OrderBy(x => x.SortOrder))
            {
                entity.Details.Add(new InspeksiKendaraanDetailEntity
                {
                    InspeksiKendaraanId = entity.Id,
                    Kategori = item.Kategori.Trim(),
                    NamaPemeriksaan = item.NamaPemeriksaan.Trim(),
                    IsOk = item.IsOk,
                    Keterangan = item.Keterangan?.Trim(),
                    SortOrder = item.SortOrder,
                    IsActive = true,
                    CreatedBy = userId
                });
            }

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(x => x.Id == entity.Id);
            return ApiResponse<InspeksiKendaraanDto>.SuccessResponse(
                MapToDto(updated), "Inspeksi kendaraan berhasil diperbarui.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var entity = await _context.InspeksiKendaraanEntities
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return ApiResponse<bool>.ErrorResponse(
                    "ERR-INSPEKSIKENDARAAN-001", "Data inspeksi kendaraan tidak ditemukan.");

            var now = DateTime.UtcNow;
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedBy = userId;

            foreach (var detail in entity.Details.Where(x => !x.IsDeleted))
            {
                detail.IsDeleted = true;
                detail.DeletedAt = now;
                detail.DeletedBy = userId;
            }

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Inspeksi kendaraan berhasil dihapus.");
        }

        private async Task<(string Code, string Message)?> ValidateReferencesAsync(
            string kendaraanId,
            string driverId)
        {
            var kendaraan = await _context.Kendaraans
                .FirstOrDefaultAsync(x => x.Id == kendaraanId && !x.IsDeleted);

            if (kendaraan == null)
                return ("ERR-INSPEKSIKENDARAAN-002", "Kendaraan tidak ditemukan.");

            if (!kendaraan.IsActive)
                return ("ERR-INSPEKSIKENDARAAN-003", "Kendaraan tidak aktif.");

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(x => x.Id == driverId && !x.IsDeleted);

            if (driver == null)
                return ("ERR-INSPEKSIKENDARAAN-004", "Driver tidak ditemukan.");

            if (!driver.IsActive)
                return ("ERR-INSPEKSIKENDARAAN-005", "Driver tidak aktif.");

            return null;
        }

        private static string? ValidateDetails(System.Collections.Generic.List<DetailValidationItem> details)
        {
            if (details.Any(x => string.IsNullOrWhiteSpace(x.Kategori)))
                return "Kategori inspeksi wajib diisi.";

            if (details.Any(x => string.IsNullOrWhiteSpace(x.NamaPemeriksaan)))
                return "Nama pemeriksaan wajib diisi.";

            if (details.GroupBy(x => x.SortOrder).Any(g => g.Count() > 1))
                return "SortOrder detail inspeksi tidak boleh duplikat.";

            return null;
        }

        private static string CalculateKelayakan(System.Collections.Generic.IEnumerable<bool> statuses)
        {
            var items = statuses.ToList();

            return items.Count > 0 && items.All(x => x)
                ? InspeksiKendaraanEntity.StatusLayakJalan
                : InspeksiKendaraanEntity.StatusTidakLayakJalan;
        }

        private static InspeksiKendaraanDto MapToDto(InspeksiKendaraanEntity entity)
        {
            return new InspeksiKendaraanDto
            {
                Id = entity.Id,
                KendaraanId = entity.KendaraanId,
                NomorPolisi = entity.Kendaraan?.NomorPolisi ?? string.Empty,
                DriverId = entity.DriverId,
                NoPekerjaDriver = entity.Driver?.NoPekerja ?? string.Empty,
                NamaDriver = entity.Driver?.NamaDriver ?? string.Empty,
                TanggalInspeksi = entity.TanggalInspeksi,
                KelayakanJalan = entity.KelayakanJalan,
                Keterangan = entity.Keterangan,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                ModifiedAt = entity.ModifiedAt,
                Details = entity.Details
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.SortOrder)
                    .Select(x => new InspeksiKendaraanDetailDto
                    {
                        Id = x.Id,
                        Kategori = x.Kategori,
                        NamaPemeriksaan = x.NamaPemeriksaan,
                        IsOk = x.IsOk,
                        Keterangan = x.Keterangan,
                        SortOrder = x.SortOrder
                    })
                    .ToList()
            };
        }

        private sealed record DetailValidationItem(string Kategori, string NamaPemeriksaan, int SortOrder);
    }
}
