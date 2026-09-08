using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;
using System.IO;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace ApiService.Application.Services
{
    public interface IDriverService
    {
        Task<ApiResponse<PagedResponse<DriverDto>>> GetAllAsync(DriverFilterRequest filter);
        Task<ApiResponse<DriverDto>> GetByIdAsync(string id);
        Task<ApiResponse<DriverDto>> CreateAsync(CreateDriverRequest request, string userId);
        Task<ApiResponse<DriverDto>> UpdateAsync(string id, UpdateDriverRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        Task<ApiResponse<DriverImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
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

        private const int DriverImportHeaderRow = 1;
        private const int DriverImportDataStartRow = 2;

        private static readonly string[] DriverRequiredImportHeaders =
        {
    "nopekerja", "namadriver", "nohp", "email", "vendor", "nopekerjaatasan"
};

        // =========================================================
        // BULK UPLOAD DRIVER
        // Template kolom: NoPekerja | NamaDriver | NoHp | Email | Vendor | NoPekerjaAtasan
        // Lookup Vendor via Name; lookup Atasan via NoPekerja (unique di master Pekerja)
        // =========================================================

        public async Task<ApiResponse<DriverImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId)
        {
            var response = new DriverImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<DriverImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<DriverImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<DriverImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildDriverImportHeaderMap(sheet, DriverImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = DriverRequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<DriverImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var parsedRows = new List<DriverImportRow>();

                for (var row = DriverImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsDriverImportRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsedRow = new DriverImportRow
                    {
                        RowNumber = row,
                        NoPekerja = GetDriverImportCellText(sheet, row, headerMap, "NoPekerja"),
                        NamaDriver = GetDriverImportCellText(sheet, row, headerMap, "NamaDriver"),
                        NoHp = GetDriverImportCellText(sheet, row, headerMap, "NoHp"),
                        Email = GetDriverImportCellText(sheet, row, headerMap, "Email"),
                        VendorName = GetDriverImportCellText(sheet, row, headerMap, "Vendor"),
                        NoPekerjaAtasan = GetDriverImportCellText(sheet, row, headerMap, "NoPekerjaAtasan"),
                    };

                    ValidateDriverRequiredField(response.Errors, row, "NoPekerja", parsedRow.NoPekerja);
                    ValidateDriverRequiredField(response.Errors, row, "NamaDriver", parsedRow.NamaDriver);
                    ValidateDriverRequiredField(response.Errors, row, "NoHp", parsedRow.NoHp);
                    ValidateDriverRequiredField(response.Errors, row, "Email", parsedRow.Email);
                    ValidateDriverRequiredField(response.Errors, row, "Vendor", parsedRow.VendorName);
                    ValidateDriverRequiredField(response.Errors, row, "NoPekerjaAtasan", parsedRow.NoPekerjaAtasan);

                    parsedRows.Add(parsedRow);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<DriverImportResponse>.BadRequest("Tidak ada data Driver pada file Excel.");

                // =========================
                // VALIDASI DUPLIKAT NO.PEKERJA DI DALAM FILE
                // =========================
                var duplicateNoPekerjaInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoPekerja))
                    .GroupBy(x => x.NoPekerja.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.NoPekerja) &&
                             duplicateNoPekerjaInFile.Contains(x.NoPekerja.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "NoPekerja",
                        Message = $"NoPekerja '{row.NoPekerja}' duplikat di dalam file Excel."
                    });
                }

                // =========================
                // VALIDASI DUPLIKAT EMAIL DI DALAM FILE
                // =========================
                var duplicateEmailInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                    .GroupBy(x => x.Email.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.Email) &&
                             duplicateEmailInFile.Contains(x.Email.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "Email",
                        Message = $"Email '{row.Email}' duplikat di dalam file Excel."
                    });
                }

                // =========================
                // VALIDASI NO.PEKERJA SUDAH TERDAFTAR DI DB
                // =========================
                var noPekerjaList = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoPekerja))
                    .Select(x => x.NoPekerja.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var existingNoPekerjas = await _context.Drivers
                    .Where(x => !x.IsDeleted && noPekerjaList.Contains(x.NoPekerja))
                    .Select(x => x.NoPekerja)
                    .ToListAsync();

                var existingNoPekerjaSet = existingNoPekerjas.ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.NoPekerja) &&
                             existingNoPekerjaSet.Contains(x.NoPekerja.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "NoPekerja",
                        Message = $"NoPekerja '{row.NoPekerja}' sudah terdaftar."
                    });
                }

                // =========================
                // VALIDASI EMAIL SUDAH TERDAFTAR DI DB
                // =========================
                var emailList = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                    .Select(x => x.Email.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var existingEmails = await _context.Drivers
                    .Where(x => !x.IsDeleted && emailList.Contains(x.Email))
                    .Select(x => x.Email)
                    .ToListAsync();

                var existingEmailSet = existingEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.Email) &&
                             existingEmailSet.Contains(x.Email.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "Email",
                        Message = $"Email '{row.Email}' sudah terdaftar."
                    });
                }

                // =========================
                // LOAD MASTER DATA SEKALI (Vendor & Pekerja sebagai Atasan)
                // =========================
                var vendors = await _context.Vendors.Where(x => !x.IsDeleted).ToListAsync();
                var pekerjas = await _context.Pekerjas.Where(x => !x.IsDeleted).ToListAsync();

                // =========================
                // VALIDASI VENDOR
                // =========================
                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.VendorName))
                        continue; // sudah ditangani ValidateDriverRequiredField

                    if (!vendors.Any(x => string.Equals(x.Name.Trim(), row.VendorName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Vendor",
                            Message = $"Vendor '{row.VendorName}' tidak ditemukan pada master Vendor."
                        });
                    }
                }

                // =========================
                // VALIDASI ATASAN (lookup via NoPekerja)
                // =========================
                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.NoPekerjaAtasan))
                        continue; // sudah ditangani ValidateDriverRequiredField

                    if (!pekerjas.Any(x => string.Equals(x.NoPekerja?.Trim(), row.NoPekerjaAtasan.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NoPekerjaAtasan",
                            Message = $"NoPekerja Atasan '{row.NoPekerjaAtasan}' tidak ditemukan pada master Pekerja."
                        });
                    }
                }

                // =========================
                // JIKA ADA ERROR VALIDASI -> TIDAK ADA YANG DISIMPAN
                // =========================
                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedDriver = 0;

                    return ApiResponse<DriverImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data Driver yang disimpan.");
                }

                // =========================
                // BUILD & SAVE
                // =========================
                var now = DateTime.UtcNow;
                var newDrivers = new List<Driver>();

                foreach (var row in parsedRows)
                {
                    var vendor = vendors.First(x =>
                        string.Equals(x.Name.Trim(), row.VendorName.Trim(), StringComparison.OrdinalIgnoreCase));

                    var atasan = pekerjas.First(x =>
                        string.Equals(x.NoPekerja?.Trim(), row.NoPekerjaAtasan.Trim(), StringComparison.OrdinalIgnoreCase));

                    var driver = new Driver
                    {
                        NoPekerja = row.NoPekerja.Trim(),
                        NamaDriver = row.NamaDriver.Trim(),
                        NoHp = row.NoHp.Trim(),
                        Email = row.Email.Trim(),
                        VendorId = vendor.Id,
                        AtasanId = atasan.Id,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newDrivers.Add(driver);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new DriverImportPreviewDto
                        {
                            NoPekerja = driver.NoPekerja,
                            NamaDriver = driver.NamaDriver,
                            NoHp = driver.NoHp,
                            Email = driver.Email,
                            VendorName = vendor.Name,
                            NamaPekerjaAtasan = atasan.NamaPekerja
                        });
                    }
                }

                await _context.Drivers.AddRangeAsync(newDrivers);
                await _context.SaveChangesAsync();

                response.InsertedDriver = newDrivers.Count;
                response.SuccessCount = newDrivers.Count;
                response.ErrorCount = 0;

                return ApiResponse<DriverImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedDriver} Driver berhasil diimport.");
            }
            catch (DbUpdateException ex)
            {
                var dbMessage = ex.InnerException?.Message ?? ex.Message;

                if (dbMessage.Contains("IX_Drivers_NoPekerja", StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = 0,
                        Column = "NoPekerja",
                        Message = "Terdapat NoPekerja yang sudah terdaftar."
                    });
                }
                else if (dbMessage.Contains("IX_Drivers_Email", StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = 0,
                        Column = "Email",
                        Message = "Terdapat Email yang sudah terdaftar."
                    });
                }

                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedDriver = 0;

                    return ApiResponse<DriverImportResponse>.SuccessResponse(
                        response,
                        "Import dibatalkan karena terdapat data duplikat.");
                }

                return ApiResponse<DriverImportResponse>.ErrorResponse(
                    "ERR-DRIVER-IMPORT-001", "Terjadi kesalahan saat menyimpan data Driver.");
            }
            catch (Exception ex)
            {
                return ApiResponse<DriverImportResponse>.ErrorResponse(
                    "ERR-DRIVER-IMPORT-002", $"Gagal mengimport data Driver: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Driver");

            var headers = new[] { "NoPekerja", "NamaDriver", "NoHp", "Email", "Vendor", "NoPekerjaAtasan" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(DriverImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(DriverImportHeaderRow, 1, DriverImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh pengisian (italic abu-abu — harus dihapus/ditimpa user)
            sheet.Cell(2, 1).Value = "DRV001";
            sheet.Cell(2, 2).Value = "Budi Santoso";
            sheet.Cell(2, 3).Value = "081234567890";
            sheet.Cell(2, 4).Value = "budi.santoso@example.com";
            sheet.Cell(2, 5).Value = "PT Maju Jaya";
            sheet.Cell(2, 6).Value = "19280027";
            sheet.Range(2, 1, 2, headers.Length).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, headers.Length).Style.Font.FontColor = XLColor.Gray;

            sheet.Columns().AdjustToContents();

            // Sheet referensi Vendor
            var vendors = await _context.Vendors
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var vendorRefSheet = workbook.Worksheets.Add("Referensi Vendor");
            vendorRefSheet.Cell(1, 1).Value = "Vendor (Valid)";
            vendorRefSheet.Cell(1, 1).Style.Font.Bold = true;
            for (var i = 0; i < vendors.Count; i++)
                vendorRefSheet.Cell(i + 2, 1).Value = vendors[i].Name;
            vendorRefSheet.Columns().AdjustToContents();

            // Sheet referensi Pekerja (sebagai Atasan) - NoPekerja + NamaPekerja
            var pekerjas = await _context.Pekerjas
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NamaPekerja)
                .ToListAsync();

            var pekerjaRefSheet = workbook.Worksheets.Add("Referensi Atasan");
            pekerjaRefSheet.Cell(1, 1).Value = "NoPekerja (Valid)";
            pekerjaRefSheet.Cell(1, 2).Value = "Nama Pekerja";
            pekerjaRefSheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
            for (var i = 0; i < pekerjas.Count; i++)
            {
                pekerjaRefSheet.Cell(i + 2, 1).Value = pekerjas[i].NoPekerja;
                pekerjaRefSheet.Cell(i + 2, 2).Value = pekerjas[i].NamaPekerja;
            }
            pekerjaRefSheet.Columns().AdjustToContents();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            var resultStream = new MemoryStream(bytes);

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_Driver.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = resultStream
            });
        }

        // =========================================================
        // HELPERS (private, khusus import Driver)
        // =========================================================

        private static Dictionary<string, int> BuildDriverImportHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(headerRow, column).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;

                var normalized = NormalizeDriverImportHeader(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, column);
            }

            return result;
        }

        private static string NormalizeDriverImportHeader(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetDriverImportCellText(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = NormalizeDriverImportHeader(header);
            if (!headerMap.TryGetValue(normalized, out var column))
                return string.Empty;

            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsDriverImportRowEmpty(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var noPekerja = GetDriverImportCellText(sheet, row, headerMap, "NoPekerja");
            var namaDriver = GetDriverImportCellText(sheet, row, headerMap, "NamaDriver");
            return string.IsNullOrWhiteSpace(noPekerja) && string.IsNullOrWhiteSpace(namaDriver);
        }

        private static void ValidateDriverRequiredField(
            List<ImportRowError> errors, int row, string column, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = row,
                    Column = column,
                    Message = $"Data wajib '{column}' kosong."
                });
            }
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