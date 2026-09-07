using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface IDriverService
    {
        Task<ApiResponse<PagedResponse<DriverDto>>> GetAllAsync(DriverFilterRequest filter);
        Task<ApiResponse<DriverDto>> GetByIdAsync(string id);
        Task<ApiResponse<DriverDto>> CreateAsync(CreateDriverRequest request, string userId);
        Task<ApiResponse<DriverDto>> UpdateAsync(string id, UpdateDriverRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class DriverService : IDriverService
    {
        private readonly IServiceDbContext _context;

        public DriverService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<Driver> BaseQuery() =>
            _context.Drivers
                .Include(d => d.Vendor)
                .Include(d => d.Atasan)
                    .ThenInclude(a => a!.Jabatan)
                .Where(d => !d.IsDeleted);

        public async Task<ApiResponse<PagedResponse<DriverDto>>> GetAllAsync(DriverFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(d =>
                    d.NoPekerja.Contains(filter.Search) ||
                    d.NamaDriver.Contains(filter.Search) ||
                    d.NoHp.Contains(filter.Search) ||
                    d.Email.Contains(filter.Search));

            if (!string.IsNullOrEmpty(filter.VendorId))
                query = query.Where(d => d.VendorId == filter.VendorId);

            if (filter.IsActive.HasValue)
                query = query.Where(d => d.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<DriverDto>>.SuccessResponse(new PagedResponse<DriverDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<DriverDto>> GetByIdAsync(string id)
        {
            var driver = await BaseQuery().FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-001", "Driver not found");

            return ApiResponse<DriverDto>.SuccessResponse(MapToDto(driver));
        }

        public async Task<ApiResponse<DriverDto>> CreateAsync(CreateDriverRequest request, string userId)
        {
            var noPekerjaExists = await _context.Drivers
                .AnyAsync(d => d.NoPekerja == request.NoPekerja && !d.IsDeleted);

            if (noPekerjaExists)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-002", "No. Pekerja sudah terdaftar");

            var emailExists = await _context.Drivers
                .AnyAsync(d => d.Email == request.Email && !d.IsDeleted);

            if (emailExists)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-003", "Email sudah terdaftar");

            var vendorOk = await _context.Vendors
                .AnyAsync(v => v.Id == request.VendorId && !v.IsDeleted);

            if (!vendorOk)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-004", "Vendor tidak ditemukan");

            var atasanOk = await _context.Pekerjas
                .AnyAsync(p => p.Id == request.AtasanId && !p.IsDeleted);

            if (!atasanOk)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-005", "Atasan tidak ditemukan");

            var driver = new Driver
            {
                NoPekerja = request.NoPekerja,
                NamaDriver = request.NamaDriver,
                NoHp = request.NoHp,
                Email = request.Email,
                VendorId = request.VendorId,
                AtasanId = request.AtasanId,
                IsActive = request.IsActive,
                CreatedBy = userId
            };

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(d => d.Id == driver.Id);
            return ApiResponse<DriverDto>.SuccessResponse(MapToDto(created), "Driver berhasil ditambahkan");
        }

        public async Task<ApiResponse<DriverDto>> UpdateAsync(string id, UpdateDriverRequest request, string userId)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (driver == null)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-001", "Driver not found");

            var noPekerjaExists = await _context.Drivers
                .AnyAsync(d => d.NoPekerja == request.NoPekerja && d.Id != id && !d.IsDeleted);

            if (noPekerjaExists)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-002", "No. Pekerja sudah terdaftar");

            var emailExists = await _context.Drivers
                .AnyAsync(d => d.Email == request.Email && d.Id != id && !d.IsDeleted);

            if (emailExists)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-003", "Email sudah terdaftar");

            var vendorOk = await _context.Vendors
                .AnyAsync(v => v.Id == request.VendorId && !v.IsDeleted);

            if (!vendorOk)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-004", "Vendor tidak ditemukan");

            var atasanOk = await _context.Pekerjas
                .AnyAsync(p => p.Id == request.AtasanId && !p.IsDeleted);

            if (!atasanOk)
                return ApiResponse<DriverDto>.ErrorResponse("ERR-DRIVER-005", "Atasan tidak ditemukan");

            driver.NoPekerja = request.NoPekerja;
            driver.NamaDriver = request.NamaDriver;
            driver.NoHp = request.NoHp;
            driver.Email = request.Email;
            driver.VendorId = request.VendorId;
            driver.AtasanId = request.AtasanId;
            driver.IsActive = request.IsActive;
            driver.ModifiedAt = DateTime.UtcNow;
            driver.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(d => d.Id == driver.Id);
            return ApiResponse<DriverDto>.SuccessResponse(MapToDto(updated), "Driver berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (driver == null)
                return ApiResponse<bool>.ErrorResponse("ERR-DRIVER-001", "Driver not found");

            driver.IsDeleted = true;
            driver.DeletedAt = DateTime.UtcNow;
            driver.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Driver deleted");
        }

        private static DriverDto MapToDto(Driver d) => new()
        {
            Id = d.Id,
            NoPekerja = d.NoPekerja,
            NamaDriver = d.NamaDriver,
            NoHp = d.NoHp,
            Email = d.Email,
            VendorId = d.VendorId,
            VendorName = d.Vendor?.Name ?? string.Empty,
            AtasanId = d.AtasanId,
            AtasanNama = d.Atasan?.NamaPekerja ?? string.Empty,
            JabatanAtasan = d.Atasan?.Jabatan?.Name ?? string.Empty,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt,
            ModifiedAt = d.ModifiedAt
        };
    }
}