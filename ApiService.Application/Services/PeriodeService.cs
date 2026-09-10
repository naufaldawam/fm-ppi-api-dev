using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface IPeriodeService
    {
        Task<ApiResponse<PagedResponse<PeriodeDto>>> GetAllAsync(PeriodeFilterRequest filter);
        Task<ApiResponse<PeriodeDto>> GetByIdAsync(string id);
        Task<ApiResponse<PeriodeDto>> CreateAsync(CreatePeriodeRequest request, string userId);
        Task<ApiResponse<PeriodeDto>> UpdateAsync(string id, UpdatePeriodeRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class PeriodeService : IPeriodeService
    {
        private readonly IServiceDbContext _context;

        public PeriodeService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<PeriodeDto>>> GetAllAsync(PeriodeFilterRequest filter)
        {
            var query = _context.Periodes
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(p => p.NamaPeriode.Contains(filter.Search));

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.TanggalAwal)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<PeriodeDto>>.SuccessResponse(new PagedResponse<PeriodeDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<PeriodeDto>> GetByIdAsync(string id)
        {
            var periode = await _context.Periodes
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (periode == null)
                return ApiResponse<PeriodeDto>.ErrorResponse("ERR-PERIODE-001", "Periode not found");

            return ApiResponse<PeriodeDto>.SuccessResponse(MapToDto(periode));
        }

        public async Task<ApiResponse<PeriodeDto>> CreateAsync(CreatePeriodeRequest request, string userId)
        {
            var exists = await _context.Periodes
                .AnyAsync(p => p.NamaPeriode == request.NamaPeriode && !p.IsDeleted);

            if (exists)
                return ApiResponse<PeriodeDto>.ErrorResponse("ERR-PERIODE-002", "Nama periode sudah ada");

            var periode = new Periode
            {
                NamaPeriode = request.NamaPeriode,
                TanggalAwal = request.TanggalAwal,
                TanggalAkhir = request.TanggalAkhir,
                IsActive = request.IsActive,
                CreatedBy = userId
            };

            _context.Periodes.Add(periode);
            await _context.SaveChangesAsync();

            return ApiResponse<PeriodeDto>.SuccessResponse(MapToDto(periode), "Periode berhasil ditambahkan");
        }

        public async Task<ApiResponse<PeriodeDto>> UpdateAsync(string id, UpdatePeriodeRequest request, string userId)
        {
            var periode = await _context.Periodes
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (periode == null)
                return ApiResponse<PeriodeDto>.ErrorResponse("ERR-PERIODE-001", "Periode not found");

            var duplicate = await _context.Periodes
                .AnyAsync(p => p.NamaPeriode == request.NamaPeriode && p.Id != id && !p.IsDeleted);

            if (duplicate)
                return ApiResponse<PeriodeDto>.ErrorResponse("ERR-PERIODE-002", "Nama periode sudah ada");

            periode.NamaPeriode = request.NamaPeriode;
            periode.TanggalAwal = request.TanggalAwal;
            periode.TanggalAkhir = request.TanggalAkhir;
            periode.IsActive = request.IsActive;
            periode.ModifiedAt = DateTime.UtcNow;
            periode.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<PeriodeDto>.SuccessResponse(MapToDto(periode), "Periode berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var periode = await _context.Periodes
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (periode == null)
                return ApiResponse<bool>.ErrorResponse("ERR-PERIODE-001", "Periode not found");

            periode.IsDeleted = true;
            periode.DeletedAt = DateTime.UtcNow;
            periode.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Periode deleted");
        }

        private static PeriodeDto MapToDto(Periode p) => new()
        {
            Id = p.Id,
            NamaPeriode = p.NamaPeriode,
            TanggalAwal = p.TanggalAwal,
            TanggalAkhir = p.TanggalAkhir,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            ModifiedAt = p.ModifiedAt
        };
    }
}