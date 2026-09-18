using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface ITagihanKwhService
    {
        /// <summary>kategori = TagihanKwh.KategoriP8 ("p8") atau nanti KategoriUmum.</summary>
        Task<ApiResponse<PagedResponse<TagihanKwhDto>>> GetAllAsync(TagihanKwhFilterRequest filter, string kategori);
        Task<ApiResponse<TagihanKwhDto>> GetByIdAsync(string id, string kategori);
        Task<ApiResponse<TagihanKwhDto>> CreateAsync(CreateTagihanKwhRequest request, string kategori, string userId);
        Task<ApiResponse<TagihanKwhDto>> UpdateAsync(string id, UpdateTagihanKwhRequest request, string kategori, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string kategori, string userId);
        Task<ApiResponse<TagihanKwhSummaryDto>> GetSummaryAsync(TagihanKwhSummaryRequest filter, string kategori);
    }

    public class TagihanKwhService : ITagihanKwhService
    {
        private readonly IServiceDbContext _context;

        public TagihanKwhService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<TagihanKwh> BaseQuery() =>
            _context.TagihanKwhs
                .Include(t => t.Periode)
                .Where(t => !t.IsDeleted);

        public async Task<ApiResponse<PagedResponse<TagihanKwhDto>>> GetAllAsync(
            TagihanKwhFilterRequest filter, string kategori)
        {
            var query = BaseQuery().Where(t => t.Kategori == kategori);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(t => t.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(t => t.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<TagihanKwhDto>>.SuccessResponse(new PagedResponse<TagihanKwhDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<TagihanKwhDto>> GetByIdAsync(string id, string kategori)
        {
            var tagihan = await BaseQuery()
                .FirstOrDefaultAsync(t => t.Id == id && t.Kategori == kategori);

            if (tagihan == null)
                return ApiResponse<TagihanKwhDto>.ErrorResponse("ERR-TAGIHANKWH-001", "Tagihan KWh not found");

            return ApiResponse<TagihanKwhDto>.SuccessResponse(MapToDto(tagihan));
        }

        public async Task<ApiResponse<TagihanKwhDto>> CreateAsync(
            CreateTagihanKwhRequest request, string kategori, string userId)
        {
            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<TagihanKwhDto>.ErrorResponse("ERR-TAGIHANKWH-007", "Periode tidak ditemukan");

            var tagihan = new TagihanKwh
            {
                PeriodeId = request.PeriodeId,
                TanggalPenagihan = request.TanggalPenagihan,
                JumlahBiaya = request.JumlahBiaya,
                JumlahKwh = request.JumlahKwh,
                // Kategori diisi server-side per endpoint (sekarang "p8")
                Kategori = kategori,
                IsActive = true,
                CreatedBy = userId
            };

            _context.TagihanKwhs.Add(tagihan);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(t => t.Id == tagihan.Id);
            return ApiResponse<TagihanKwhDto>.SuccessResponse(MapToDto(created), "Tagihan KWh berhasil ditambahkan");
        }

        public async Task<ApiResponse<TagihanKwhDto>> UpdateAsync(
            string id, UpdateTagihanKwhRequest request, string kategori, string userId)
        {
            var tagihan = await _context.TagihanKwhs
                .FirstOrDefaultAsync(t => t.Id == id && t.Kategori == kategori && !t.IsDeleted);

            if (tagihan == null)
                return ApiResponse<TagihanKwhDto>.ErrorResponse("ERR-TAGIHANKWH-001", "Tagihan KWh not found");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<TagihanKwhDto>.ErrorResponse("ERR-TAGIHANKWH-007", "Periode tidak ditemukan");

            tagihan.PeriodeId = request.PeriodeId;
            tagihan.TanggalPenagihan = request.TanggalPenagihan;
            tagihan.JumlahBiaya = request.JumlahBiaya;
            tagihan.JumlahKwh = request.JumlahKwh;
            tagihan.Kategori = kategori;
            tagihan.IsActive = request.IsActive;
            tagihan.ModifiedAt = DateTime.UtcNow;
            tagihan.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(t => t.Id == tagihan.Id);
            return ApiResponse<TagihanKwhDto>.SuccessResponse(MapToDto(updated), "Tagihan KWh berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string kategori, string userId)
        {
            var tagihan = await _context.TagihanKwhs
                .FirstOrDefaultAsync(t => t.Id == id && t.Kategori == kategori && !t.IsDeleted);

            if (tagihan == null)
                return ApiResponse<bool>.ErrorResponse("ERR-TAGIHANKWH-001", "Tagihan KWh not found");

            tagihan.IsDeleted = true;
            tagihan.DeletedAt = DateTime.UtcNow;
            tagihan.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Tagihan KWh deleted");
        }

        public async Task<ApiResponse<TagihanKwhSummaryDto>> GetSummaryAsync(
            TagihanKwhSummaryRequest filter, string kategori)
        {
            string? namaPeriode = null;

            if (!string.IsNullOrEmpty(filter.PeriodeId))
            {
                namaPeriode = await _context.Periodes
                    .Where(p => p.Id == filter.PeriodeId && !p.IsDeleted)
                    .Select(p => p.NamaPeriode)
                    .FirstOrDefaultAsync();

                if (namaPeriode == null)
                    return ApiResponse<TagihanKwhSummaryDto>.NotFound("Periode tidak ditemukan");
            }

            var query = _context.TagihanKwhs.Where(t => !t.IsDeleted && t.Kategori == kategori);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(t => t.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(t => t.IsActive == filter.IsActive.Value);

            var totalMemberTagihan = await query.CountAsync();
            var grandTotalBiaya = totalMemberTagihan == 0 ? 0m : await query.SumAsync(t => t.JumlahBiaya);
            var grandTotalKwh = totalMemberTagihan == 0 ? 0m : await query.SumAsync(t => t.JumlahKwh);

            return ApiResponse<TagihanKwhSummaryDto>.SuccessResponse(new TagihanKwhSummaryDto
            {
                PeriodeId = filter.PeriodeId,
                NamaPeriode = namaPeriode,
                TotalMemberTagihan = totalMemberTagihan,
                GrandTotalBiaya = grandTotalBiaya,
                GrandTotalKwh = grandTotalKwh
            });
        }

        private static TagihanKwhDto MapToDto(TagihanKwh t) => new()
        {
            Id = t.Id,
            PeriodeId = t.PeriodeId,
            NamaPeriode = t.Periode?.NamaPeriode ?? string.Empty,
            TanggalPenagihan = t.TanggalPenagihan,
            JumlahBiaya = t.JumlahBiaya,
            JumlahKwh = t.JumlahKwh,
            Kategori = t.Kategori,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            ModifiedAt = t.ModifiedAt
        };
    }
}