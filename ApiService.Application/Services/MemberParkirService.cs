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

            // RF.ID dihandle server-side: client boleh kosong, server yang ambil/validasi
            var resolution = await ResolveRfIdCodeAsync(pekerja, request.RfIdCode);
            if (resolution.ErrorMessage != null)
                return ApiResponse<MemberParkirDto>.ErrorResponse(resolution.ErrorCode!, resolution.ErrorMessage);

            var rfidCodeExists = await _context.MemberParkirs
                .AnyAsync(m => m.RfIdCode == resolution.Code && !m.IsDeleted);

            if (rfidCodeExists)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-004", "RF.ID sudah terdaftar sebagai member parkir");

            var member = new MemberParkir
            {
                PekerjaId = request.PekerjaId,
                // Jabatan + RF.ID otomatis dihandle server-side (client tidak bisa diset)
                JabatanId = pekerja.JabatanId,
                RfIdCode = resolution.Code,
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

            // RF.ID dihandle server-side: client boleh kosong, server yang ambil/validasi
            var resolution = await ResolveRfIdCodeAsync(pekerja, request.RfIdCode);
            if (resolution.ErrorMessage != null)
                return ApiResponse<MemberParkirDto>.ErrorResponse(resolution.ErrorCode!, resolution.ErrorMessage);

            var rfidCodeExists = await _context.MemberParkirs
                .AnyAsync(m => m.RfIdCode == resolution.Code && m.Id != id && !m.IsDeleted);

            if (rfidCodeExists)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-004", "RF.ID sudah terdaftar sebagai member parkir");

            member.PekerjaId = request.PekerjaId;
            // Jabatan + RF.ID otomatis mengikuti Pekerja (perubahan tidak dari client)
            member.JabatanId = pekerja.JabatanId;
            member.RfIdCode = resolution.Code;
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

        private sealed class RfIdResolution
        {
            public string Code { get; set; } = string.Empty;
            public string? ErrorCode { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// RF.ID dihandle server-side:
        /// - Client diisi kode -> server validasi kode ter-assign ke pekerja ini (via master RfIds).
        /// - Client kosong      -> server ambil kode pertama dari Pekerja.RfIds.
        /// </summary>
        private async Task<RfIdResolution> ResolveRfIdCodeAsync(Pekerja pekerja, string? requestCode)
        {
            var trimmed = string.IsNullOrWhiteSpace(requestCode) ? string.Empty : requestCode.Trim();

            if (!string.IsNullOrEmpty(trimmed))
            {
                var master = await _context.RfIds
                    .FirstOrDefaultAsync(r => r.RfIdCode == trimmed && !r.IsDeleted);

                if (master != null && master.PekerjaId != pekerja.Id)
                {
                    return new RfIdResolution
                    {
                        ErrorCode = "ERR-MEMBERPARKIR-005",
                        ErrorMessage = master.PekerjaId == null
                            ? $"RF.ID '{trimmed}' belum di-assign ke Pekerja '{pekerja.NoPekerja}'."
                            : $"RF.ID '{trimmed}' sudah di-assign ke Pekerja lain, tidak bisa dipakai."
                    };
                }

                return new RfIdResolution { Code = trimmed };
            }

            if (pekerja.RfIds.Count > 0)
                return new RfIdResolution { Code = pekerja.RfIds[0].Trim() };

            return new RfIdResolution
            {
                ErrorCode = "ERR-MEMBERPARKIR-006",
                ErrorMessage = $"Pekerja '{pekerja.NoPekerja}' belum memiliki RF.ID di-assign. Kode RF.ID wajib diisi."
            };
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