using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface IMemberParkirService
    {
        Task<ApiResponse<PagedResponse<MemberParkirDto>>> GetAllAsync(MemberParkirFilterRequest filter);
        Task<ApiResponse<MemberParkirDto>> GetByIdAsync(string id);
        Task<ApiResponse<MemberParkirDto>> CreateAsync(CreateMemberParkirRequest request, string userId);
        Task<ApiResponse<MemberParkirDto>> UpdateAsync(string id, UpdateMemberParkirRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class MemberParkirService : IMemberParkirService
    {
        private readonly IServiceDbContext _context;

        public MemberParkirService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<MemberParkir> BaseQuery() =>
            _context.MemberParkirs
                .Include(m => m.Pekerja)
                    .ThenInclude(p => p!.Jabatan)
                .Include(m => m.Jabatan)
                .Where(m => !m.IsDeleted);

        public async Task<ApiResponse<PagedResponse<MemberParkirDto>>> GetAllAsync(MemberParkirFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(m =>
                    m.RfIdCode.Contains(filter.Search) ||
                    (m.Pekerja != null && m.Pekerja.NoPekerja.Contains(filter.Search)) ||
                    (m.Pekerja != null && m.Pekerja.NamaPekerja.Contains(filter.Search)));

            if (!string.IsNullOrEmpty(filter.JabatanId))
                query = query.Where(m => m.JabatanId == filter.JabatanId);

            if (filter.IsActive.HasValue)
                query = query.Where(m => m.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<MemberParkirDto>>.SuccessResponse(new PagedResponse<MemberParkirDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<MemberParkirDto>> GetByIdAsync(string id)
        {
            var member = await BaseQuery().FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-001", "Member parkir not found");

            return ApiResponse<MemberParkirDto>.SuccessResponse(MapToDto(member));
        }

        public async Task<ApiResponse<MemberParkirDto>> CreateAsync(CreateMemberParkirRequest request, string userId)
        {
            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-003", "Pekerja tidak ditemukan");

            var rfidCodeExists = await _context.MemberParkirs
                .AnyAsync(m => m.RfIdCode == request.RfIdCode && !m.IsDeleted);

            if (rfidCodeExists)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-004", "RF.ID sudah terdaftar sebagai member parkir");

            var rfidConflict = await ValidateRfIdBelongsToPekerjaAsync(request.RfIdCode, request.PekerjaId);
            if (rfidConflict != null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-005", rfidConflict);

            var member = new MemberParkir
            {
                PekerjaId = request.PekerjaId,
                // Jabatan otomatis diisi dari Pekerja (client tidak bisa diset)
                JabatanId = pekerja.JabatanId,
                RfIdCode = request.RfIdCode,
                TanggalPenagihan = request.TanggalPenagihan,
                JumlahBiaya = request.JumlahBiaya,
                IsActive = true,
                CreatedBy = userId
            };

            _context.MemberParkirs.Add(member);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(m => m.Id == member.Id);
            return ApiResponse<MemberParkirDto>.SuccessResponse(MapToDto(created), "Member parkir berhasil ditambahkan");
        }

        public async Task<ApiResponse<MemberParkirDto>> UpdateAsync(string id, UpdateMemberParkirRequest request, string userId)
        {
            var member = await _context.MemberParkirs
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (member == null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-001", "Member parkir not found");

            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-003", "Pekerja tidak ditemukan");

            var rfidCodeExists = await _context.MemberParkirs
                .AnyAsync(m => m.RfIdCode == request.RfIdCode && m.Id != id && !m.IsDeleted);

            if (rfidCodeExists)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-004", "RF.ID sudah terdaftar sebagai member parkir");

            var rfidConflict = await ValidateRfIdBelongsToPekerjaAsync(request.RfIdCode, request.PekerjaId);
            if (rfidConflict != null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-005", rfidConflict);

            member.PekerjaId = request.PekerjaId;
            // Jabatan otomatis mengikuti Pekerja (perubahan jahitan tidak dari client)
            member.JabatanId = pekerja.JabatanId;
            member.RfIdCode = request.RfIdCode;
            member.TanggalPenagihan = request.TanggalPenagihan;
            member.JumlahBiaya = request.JumlahBiaya;
            member.IsActive = request.IsActive;
            member.ModifiedAt = DateTime.UtcNow;
            member.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(m => m.Id == member.Id);
            return ApiResponse<MemberParkirDto>.SuccessResponse(MapToDto(updated), "Member parkir berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var member = await _context.MemberParkirs
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (member == null)
                return ApiResponse<bool>.ErrorResponse("ERR-MEMBERPARKIR-001", "Member parkir not found");

            member.IsDeleted = true;
            member.DeletedAt = DateTime.UtcNow;
            member.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Member parkir deleted");
        }

        /// <summary>
        /// Cek: kalau kode RF.ID ada di master RfIds DAN sudah di-assign ke Pekerja lain -> error.
        /// Kalau kartu belum ada / belum di-assign -> OK (FE boleh tipa manual).
        /// </summary>
        private async Task<string?> ValidateRfIdBelongsToPekerjaAsync(string rfidCode, string pekerjaId)
        {
            var rfid = await _context.RfIds
                .FirstOrDefaultAsync(r => r.RfIdCode == rfidCode && !r.IsDeleted);

            if (rfid == null)
                return null;

            if (!string.IsNullOrEmpty(rfid.PekerjaId) && rfid.PekerjaId != pekerjaId)
                return $"RF.ID '{rfidCode}' sudah di-assign ke Pekerja lain, tidak bisa dipakai.";

            return null;
        }

        private static MemberParkirDto MapToDto(MemberParkir m) => new()
        {
            Id = m.Id,
            PekerjaId = m.PekerjaId,
            NoPekerja = m.Pekerja?.NoPekerja ?? string.Empty,
            NamaPekerja = m.Pekerja?.NamaPekerja ?? string.Empty,
            JabatanId = m.JabatanId,
            JabatanName = m.Jabatan?.Name ?? m.Pekerja?.Jabatan?.Name ?? string.Empty,
            RfIdCode = m.RfIdCode,
            TanggalPenagihan = m.TanggalPenagihan,
            JumlahBiaya = m.JumlahBiaya,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt,
            ModifiedAt = m.ModifiedAt
        };
    }
}