using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface IRfIdService
    {
        Task<ApiResponse<PagedResponse<RfIdDto>>> GetAllAsync(RfIdFilterRequest filter);
        Task<ApiResponse<RfIdDto>> GetByIdAsync(string id);
        Task<ApiResponse<RfIdDto>> CreateAsync(CreateRfIdRequest request, string userId);
        Task<ApiResponse<RfIdDto>> UpdateAsync(string id, UpdateRfIdRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class RfIdService : IRfIdService
    {
        private readonly IServiceDbContext _context;

        public RfIdService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<RfId> BaseQuery() =>
            _context.RfIds
                .Include(r => r.Pekerja)
                .Include(r => r.Kendaraan)
                .Where(r => !r.IsDeleted);

        public async Task<ApiResponse<PagedResponse<RfIdDto>>> GetAllAsync(RfIdFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(r =>
                    r.RfIdCode.Contains(filter.Search) ||
                    (r.Pekerja != null && r.Pekerja.NoPekerja.Contains(filter.Search)) ||
                    (r.Pekerja != null && r.Pekerja.NamaPekerja.Contains(filter.Search)) ||
                    (r.Kendaraan != null && r.Kendaraan.NomorPolisi.Contains(filter.Search)));

            if (!string.IsNullOrEmpty(filter.PekerjaId))
                query = query.Where(r => r.PekerjaId == filter.PekerjaId);

            if (!string.IsNullOrEmpty(filter.KendaraanId))
                query = query.Where(r => r.KendaraanId == filter.KendaraanId);

            if (filter.IsActive.HasValue)
                query = query.Where(r => r.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<RfIdDto>>.SuccessResponse(new PagedResponse<RfIdDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<RfIdDto>> GetByIdAsync(string id)
        {
            var rfid = await BaseQuery().FirstOrDefaultAsync(r => r.Id == id);

            if (rfid == null)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-001", "RF.ID not found");

            return ApiResponse<RfIdDto>.SuccessResponse(MapToDto(rfid));
        }

        public async Task<ApiResponse<RfIdDto>> CreateAsync(CreateRfIdRequest request, string userId)
        {
            var codeExists = await _context.RfIds
                .AnyAsync(r => r.RfIdCode == request.RfIdCode && !r.IsDeleted);

            if (codeExists)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-002", "RF.ID sudah terdaftar");

            var pekerja = await _context.Pekerjas
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-003", "Pekerja tidak ditemukan");

            var kendaraanOk = await _context.Kendaraans
                .AnyAsync(k => k.Id == request.KendaraanId && !k.IsDeleted);

            if (!kendaraanOk)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-004", "Kendaraan (Nopol) tidak ditemukan");

            var rfid = new RfId
            {
                RfIdCode = request.RfIdCode,
                PekerjaId = request.PekerjaId,
                KendaraanId = request.KendaraanId,
                IsActive = request.IsActive,
                CreatedBy = userId
            };

            _context.RfIds.Add(rfid);

            // Sinkronkan ke Pekerja.RfIds
            AddCodeToPekerja(pekerja, request.RfIdCode);

            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(r => r.Id == rfid.Id);
            return ApiResponse<RfIdDto>.SuccessResponse(MapToDto(created), "RF.ID berhasil ditambahkan");
        }

        public async Task<ApiResponse<RfIdDto>> UpdateAsync(string id, UpdateRfIdRequest request, string userId)
        {
            var rfid = await _context.RfIds
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (rfid == null)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-001", "RF.ID not found");

            var codeExists = await _context.RfIds
                .AnyAsync(r => r.RfIdCode == request.RfIdCode && r.Id != id && !r.IsDeleted);

            if (codeExists)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-002", "RF.ID sudah terdaftar");

            var newPekerja = await _context.Pekerjas
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (newPekerja == null)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-003", "Pekerja tidak ditemukan");

            var kendaraanOk = await _context.Kendaraans
                .AnyAsync(k => k.Id == request.KendaraanId && !k.IsDeleted);

            if (!kendaraanOk)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-004", "Kendaraan (Nopol) tidak ditemukan");

            var oldCode = rfid.RfIdCode;
            var oldPekerjaId = rfid.PekerjaId;

            // Lepas assignment lama & pasang assignment baru kalau Pekerja atau kodenya berubah
            if (oldPekerjaId != request.PekerjaId || oldCode != request.RfIdCode)
            {
                var oldPekerja = oldPekerjaId == request.PekerjaId
                    ? newPekerja
                    : await _context.Pekerjas.FirstOrDefaultAsync(p => p.Id == oldPekerjaId);

                if (oldPekerja != null)
                    RemoveCodeFromPekerja(oldPekerja, oldCode);

                AddCodeToPekerja(newPekerja, request.RfIdCode);
            }

            rfid.RfIdCode = request.RfIdCode;
            rfid.PekerjaId = request.PekerjaId;
            rfid.KendaraanId = request.KendaraanId;
            rfid.IsActive = request.IsActive;
            rfid.ModifiedAt = DateTime.UtcNow;
            rfid.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(r => r.Id == rfid.Id);
            return ApiResponse<RfIdDto>.SuccessResponse(MapToDto(updated), "RF.ID berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var rfid = await _context.RfIds
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (rfid == null)
                return ApiResponse<bool>.ErrorResponse("ERR-RFID-001", "RF.ID not found");

            var pekerja = await _context.Pekerjas.FirstOrDefaultAsync(p => p.Id == rfid.PekerjaId);
            if (pekerja != null)
                RemoveCodeFromPekerja(pekerja, rfid.RfIdCode);

            rfid.IsDeleted = true;
            rfid.DeletedAt = DateTime.UtcNow;
            rfid.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "RF.ID deleted");
        }

        // ------------------------------------------------------------
        // Helper sinkronisasi Pekerja.RfIds
        // ------------------------------------------------------------
        private static void AddCodeToPekerja(Pekerja pekerja, string code)
        {
            if (!pekerja.RfIds.Contains(code))
                pekerja.RfIds.Add(code);
        }

        private static void RemoveCodeFromPekerja(Pekerja pekerja, string code)
        {
            pekerja.RfIds.Remove(code);
        }

        private static RfIdDto MapToDto(RfId r) => new()
        {
            Id = r.Id,
            RfIdCode = r.RfIdCode,
            PekerjaId = r.PekerjaId,
            NoPekerja = r.Pekerja?.NoPekerja ?? string.Empty,
            NamaPekerja = r.Pekerja?.NamaPekerja ?? string.Empty,
            KendaraanId = r.KendaraanId,
            NomorPolisi = r.Kendaraan?.NomorPolisi ?? string.Empty,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            ModifiedAt = r.ModifiedAt
        };
    }
}