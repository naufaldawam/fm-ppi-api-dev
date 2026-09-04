using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface IKendaraanService
    {
        Task<ApiResponse<PagedResponse<KendaraanDto>>> GetAllAsync(KendaraanFilterRequest filter);
        Task<ApiResponse<List<KendaraanLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<KendaraanDto>> GetByIdAsync(string id);
        Task<ApiResponse<KendaraanDto>> CreateAsync(CreateKendaraanRequest request, string userId);
        Task<ApiResponse<KendaraanDto>> UpdateAsync(string id, UpdateKendaraanRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        
    }

    public class KendaraanService : IKendaraanService
    {
        private readonly IServiceDbContext _context;

        public KendaraanService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<Kendaraan> BaseQuery() =>
            _context.Kendaraans
                .Include(k => k.Tipe)
                .Include(k => k.BahanBakar)
                .Include(k => k.Vendor)
                .Include(k => k.Kepemilikan)
                .Include(k => k.Jabatan)
                .Include(k => k.Pekerja)
                .Where(k => !k.IsDeleted);

        public async Task<ApiResponse<PagedResponse<KendaraanDto>>> GetAllAsync(KendaraanFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(k =>
                    k.NomorPolisi.Contains(filter.Search) ||
                    k.Merek.Contains(filter.Search));

            if (!string.IsNullOrEmpty(filter.TipeId))
                query = query.Where(k => k.TipeId == filter.TipeId);

            if (!string.IsNullOrEmpty(filter.BahanBakarId))
                query = query.Where(k => k.BahanBakarId == filter.BahanBakarId);

            if (!string.IsNullOrEmpty(filter.VendorId))
                query = query.Where(k => k.VendorId == filter.VendorId);

            if (!string.IsNullOrEmpty(filter.KepemilikanId))
                query = query.Where(k => k.KepemilikanId == filter.KepemilikanId);

            if (!string.IsNullOrEmpty(filter.JabatanId))
                query = query.Where(k => k.JabatanId == filter.JabatanId);

            if (filter.IsActive.HasValue)
                query = query.Where(k => k.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(k => k.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<KendaraanDto>>.SuccessResponse(new PagedResponse<KendaraanDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<List<KendaraanLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.Kendaraans
                .Where(k => !k.IsDeleted);

            if (activeOnly)
                query = query.Where(k => k.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(k => k.NomorPolisi.Contains(search));

            var items = await query
                .OrderBy(k => k.NomorPolisi)
                .Select(k => new KendaraanLookupDto
                {
                    Id = k.Id,
                    NomorPolisi = k.NomorPolisi
                })
                .ToListAsync();

            return ApiResponse<List<KendaraanLookupDto>>.SuccessResponse(items);
        }
        public async Task<ApiResponse<KendaraanDto>> GetByIdAsync(string id)
        {
            var kendaraan = await BaseQuery().FirstOrDefaultAsync(k => k.Id == id);

            if (kendaraan == null)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-001", "Kendaraan not found");

            return ApiResponse<KendaraanDto>.SuccessResponse(MapToDto(kendaraan));
        }

        public async Task<ApiResponse<KendaraanDto>> CreateAsync(CreateKendaraanRequest request, string userId)
        {
            var nomorPolisiExists = await _context.Kendaraans
                .AnyAsync(k => k.NomorPolisi == request.NomorPolisi && !k.IsDeleted);

            if (nomorPolisiExists)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-002", "Nomor polisi sudah terdaftar");

            var refError = await ValidateReferencesAsync(
                request.TipeId, request.BahanBakarId, request.VendorId,
                request.KepemilikanId, request.JabatanId, request.PekerjaId);

            if (refError != null)
                return ApiResponse<KendaraanDto>.ErrorResponse(refError.Value.code, refError.Value.message);

            var kendaraan = new Kendaraan
            {
                NomorPolisi = request.NomorPolisi,
                TipeId = request.TipeId,
                BahanBakarId = request.BahanBakarId,
                Merek = request.Merek,
                VendorId = request.VendorId,
                KepemilikanId = request.KepemilikanId,
                JabatanId = request.JabatanId,
                PekerjaId = string.IsNullOrEmpty(request.PekerjaId) ? null : request.PekerjaId,
                IsActive = true,
                CreatedBy = userId
            };

            _context.Kendaraans.Add(kendaraan);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(k => k.Id == kendaraan.Id);
            return ApiResponse<KendaraanDto>.SuccessResponse(MapToDto(created), "Kendaraan berhasil ditambahkan");
        }

        public async Task<ApiResponse<KendaraanDto>> UpdateAsync(string id, UpdateKendaraanRequest request, string userId)
        {
            var kendaraan = await _context.Kendaraans
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);

            if (kendaraan == null)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-001", "Kendaraan not found");

            var nomorPolisiExists = await _context.Kendaraans
                .AnyAsync(k => k.NomorPolisi == request.NomorPolisi && k.Id != id && !k.IsDeleted);

            if (nomorPolisiExists)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-002", "Nomor polisi sudah terdaftar");

            var refError = await ValidateReferencesAsync(
                request.TipeId, request.BahanBakarId, request.VendorId,
                request.KepemilikanId, request.JabatanId, request.PekerjaId);

            if (refError != null)
                return ApiResponse<KendaraanDto>.ErrorResponse(refError.Value.code, refError.Value.message);

            kendaraan.NomorPolisi = request.NomorPolisi;
            kendaraan.TipeId = request.TipeId;
            kendaraan.BahanBakarId = request.BahanBakarId;
            kendaraan.Merek = request.Merek;
            kendaraan.VendorId = request.VendorId;
            kendaraan.KepemilikanId = request.KepemilikanId;
            kendaraan.JabatanId = request.JabatanId;
            kendaraan.PekerjaId = string.IsNullOrEmpty(request.PekerjaId) ? null : request.PekerjaId;
            kendaraan.IsActive = request.IsActive;
            kendaraan.ModifiedAt = DateTime.UtcNow;
            kendaraan.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(k => k.Id == kendaraan.Id);
            return ApiResponse<KendaraanDto>.SuccessResponse(MapToDto(updated), "Kendaraan berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var kendaraan = await _context.Kendaraans
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);

            if (kendaraan == null)
                return ApiResponse<bool>.ErrorResponse("ERR-KENDARAAN-001", "Kendaraan not found");

            kendaraan.IsDeleted = true;
            kendaraan.DeletedAt = DateTime.UtcNow;
            kendaraan.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Kendaraan deleted");
        }

        /// <summary>
        /// Memastikan semua referensi master data (Tipe, BahanBakar, Vendor, Kepemilikan,
        /// Jabatan) valid dan tidak terhapus, dan Pekerja (jika diisi) juga valid.
        /// </summary>
        private async Task<(string code, string message)?> ValidateReferencesAsync(
            string tipeId, string bahanBakarId, string vendorId,
            string kepemilikanId, string jabatanId, string? pekerjaId)
        {
            var tipeOk = await _context.Tipes.AnyAsync(t => t.Id == tipeId && !t.IsDeleted);
            if (!tipeOk)
                return ("ERR-KENDARAAN-003", "Tipe tidak ditemukan");

            var bahanBakarOk = await _context.BahanBakars.AnyAsync(b => b.Id == bahanBakarId && !b.IsDeleted);
            if (!bahanBakarOk)
                return ("ERR-KENDARAAN-004", "Bahan bakar tidak ditemukan");

            var vendorOk = await _context.Vendors.AnyAsync(v => v.Id == vendorId && !v.IsDeleted);
            if (!vendorOk)
                return ("ERR-KENDARAAN-005", "Vendor tidak ditemukan");

            var kepemilikanOk = await _context.Kepemilikans.AnyAsync(k => k.Id == kepemilikanId && !k.IsDeleted);
            if (!kepemilikanOk)
                return ("ERR-KENDARAAN-006", "Kepemilikan tidak ditemukan");

            var jabatanOk = await _context.Jabatans.AnyAsync(j => j.Id == jabatanId && !j.IsDeleted);
            if (!jabatanOk)
                return ("ERR-KENDARAAN-007", "Alokasi jabatan tidak ditemukan");

            if (!string.IsNullOrEmpty(pekerjaId))
            {
                var pekerjaOk = await _context.Pekerjas.AnyAsync(p => p.Id == pekerjaId && !p.IsDeleted);
                if (!pekerjaOk)
                    return ("ERR-KENDARAAN-008", "Pejabat (pekerja) tidak ditemukan");
            }

            return null;
        }

        private static KendaraanDto MapToDto(Kendaraan k) => new()
        {
            Id = k.Id,
            NomorPolisi = k.NomorPolisi,
            TipeId = k.TipeId,
            TipeName = k.Tipe?.Name ?? string.Empty,
            BahanBakarId = k.BahanBakarId,
            BahanBakarName = k.BahanBakar?.Name ?? string.Empty,
            Merek = k.Merek,
            VendorId = k.VendorId,
            VendorName = k.Vendor?.Name ?? string.Empty,
            KepemilikanId = k.KepemilikanId,
            KepemilikanName = k.Kepemilikan?.Name ?? string.Empty,
            JabatanId = k.JabatanId,
            JabatanName = k.Jabatan?.Name ?? string.Empty,
            PekerjaId = k.PekerjaId,
            PekerjaName = k.Pekerja?.NamaPekerja,
            IsActive = k.IsActive,
            CreatedAt = k.CreatedAt,
            ModifiedAt = k.ModifiedAt
        };
    }
}