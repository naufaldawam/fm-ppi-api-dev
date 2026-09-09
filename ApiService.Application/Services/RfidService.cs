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
    public interface IRfIdService
    {
        Task<ApiResponse<PagedResponse<RfIdDto>>> GetAllAsync(RfIdFilterRequest filter);
        Task<ApiResponse<RfIdDto>> GetByIdAsync(string id);
        Task<ApiResponse<RfIdDto>> CreateAsync(CreateRfIdRequest request, string userId);
        Task<ApiResponse<RfIdDto>> UpdateAsync(string id, UpdateRfIdRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        Task<ApiResponse<RfIdImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
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

            // Pekerja opsional - RF.ID boleh belum di-assign (mis. kartu masih stok / atasan kosong sementara)
            Pekerja? pekerja = null;
            if (!string.IsNullOrEmpty(request.PekerjaId))
            {
                pekerja = await _context.Pekerjas
                    .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

                if (pekerja == null)
                    return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-003", "Pekerja tidak ditemukan");
            }

            var kendaraanOk = await _context.Kendaraans
                .AnyAsync(k => k.Id == request.KendaraanId && !k.IsDeleted);

            if (!kendaraanOk)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-004", "Kendaraan (Nopol) tidak ditemukan");

            var rfid = new RfId
            {
                RfIdCode = request.RfIdCode,
                PekerjaId = string.IsNullOrEmpty(request.PekerjaId) ? null : request.PekerjaId,
                KendaraanId = request.KendaraanId,
                IsActive = request.IsActive,
                CreatedBy = userId
            };

            _context.RfIds.Add(rfid);

            // Sinkronkan ke Pekerja.RfIds (hanya kalau ada pekerja yang di-assign)
            if (pekerja != null)
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

            // Pekerja opsional - RF.ID boleh dilepas assignment-nya (jadi tidak dipegang siapapun)
            Pekerja? newPekerja = null;
            if (!string.IsNullOrEmpty(request.PekerjaId))
            {
                newPekerja = await _context.Pekerjas
                    .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

                if (newPekerja == null)
                    return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-003", "Pekerja tidak ditemukan");
            }

            var kendaraanOk = await _context.Kendaraans
                .AnyAsync(k => k.Id == request.KendaraanId && !k.IsDeleted);

            if (!kendaraanOk)
                return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-004", "Kendaraan (Nopol) tidak ditemukan");

            // Cek referensi sebelum nonaktifkan (saat ini belum ada entity lain yang
            // mereferensikan RF.ID, tapi cek ini disiapkan untuk future-proofing)
            if (rfid.IsActive && !request.IsActive)
            {
                var blocker = await GetDeactivationBlockerAsync(rfid.Id);
                if (blocker != null)
                    return ApiResponse<RfIdDto>.ErrorResponse("ERR-RFID-005", blocker);
            }

            var oldCode = rfid.RfIdCode;
            var oldPekerjaId = rfid.PekerjaId;
            var newPekerjaId = string.IsNullOrEmpty(request.PekerjaId) ? null : request.PekerjaId;

            // Lepas assignment lama & pasang assignment baru kalau Pekerja atau kodenya berubah
            if (oldPekerjaId != newPekerjaId || oldCode != request.RfIdCode)
            {
                var oldPekerja = oldPekerjaId == newPekerjaId
                    ? newPekerja
                    : (string.IsNullOrEmpty(oldPekerjaId)
                        ? null
                        : await _context.Pekerjas.FirstOrDefaultAsync(p => p.Id == oldPekerjaId));

                if (oldPekerja != null)
                    RemoveCodeFromPekerja(oldPekerja, oldCode);

                if (newPekerja != null)
                    AddCodeToPekerja(newPekerja, request.RfIdCode);
            }

            rfid.RfIdCode = request.RfIdCode;
            rfid.PekerjaId = newPekerjaId;
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

            if (!string.IsNullOrEmpty(rfid.PekerjaId))
            {
                var pekerja = await _context.Pekerjas.FirstOrDefaultAsync(p => p.Id == rfid.PekerjaId);
                if (pekerja != null)
                    RemoveCodeFromPekerja(pekerja, rfid.RfIdCode);
            }

            rfid.IsDeleted = true;
            rfid.DeletedAt = DateTime.UtcNow;
            rfid.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "RF.ID deleted");
        }

        /// <summary>
        /// Cek apakah RF.ID ini masih dipakai sebagai reference oleh entity lain sebelum
        /// boleh dinonaktifkan. Saat ini belum ada entity lain yang mereferensikan RF.ID,
        /// jadi selalu null - disiapkan untuk future-proofing.
        /// </summary>
        private Task<string?> GetDeactivationBlockerAsync(string rfIdId)
        {
            return Task.FromResult<string?>(null);
        }

        // =========================================================
        // BULK UPLOAD RF.ID
        // Template kolom: RF.ID | No.Pekerja | Nopol
        // (pakai No.Pekerja & Nopol sebagai key lookup karena keduanya unique)
        // =========================================================

        private const int RfIdImportHeaderRow = 1;
        private const int RfIdImportDataStartRow = 2;

        private static readonly string[] RfIdRequiredImportHeaders =
        {
            "rfid", "nopekerja", "nopol"
        };

        public async Task<ApiResponse<RfIdImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId)
        {
            var response = new RfIdImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<RfIdImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<RfIdImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<RfIdImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildImportHeaderMap(sheet, RfIdImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = RfIdRequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<RfIdImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var parsedRows = new List<RfIdImportRow>();

                for (var row = RfIdImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsRfIdImportRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsedRow = new RfIdImportRow
                    {
                        RowNumber = row,
                        RfIdCode = GetImportCellText(sheet, row, headerMap, "RF.ID"),
                        NoPekerja = GetImportCellText(sheet, row, headerMap, "No.Pekerja"),
                        Nopol = GetImportCellText(sheet, row, headerMap, "Nopol")
                    };

                    ValidateRequiredImportField(response.Errors, row, "RF.ID", parsedRow.RfIdCode);
                    // No.Pekerja sengaja TIDAK wajib diisi - boleh kosong (rfid belum di-assign)
                    ValidateRequiredImportField(response.Errors, row, "Nopol", parsedRow.Nopol);

                    parsedRows.Add(parsedRow);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<RfIdImportResponse>.BadRequest("Tidak ada data RF.ID pada file Excel.");

                // =========================
                // VALIDASI DUPLIKAT RF.ID DI DALAM FILE
                // =========================
                var duplicateCodeInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.RfIdCode))
                    .GroupBy(x => x.RfIdCode.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x => !string.IsNullOrWhiteSpace(x.RfIdCode) &&
                            duplicateCodeInFile.Contains(x.RfIdCode.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "RF.ID",
                        Message = $"RF.ID '{row.RfIdCode}' duplikat di dalam file Excel."
                    });
                }

                // =========================
                // VALIDASI RF.ID SUDAH TERDAFTAR DI DB
                // =========================
                var codeList = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.RfIdCode))
                    .Select(x => x.RfIdCode.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var existingCodes = await _context.RfIds
                    .Where(x => !x.IsDeleted && codeList.Contains(x.RfIdCode))
                    .Select(x => x.RfIdCode)
                    .ToListAsync();

                var existingCodeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.RfIdCode) &&
                             existingCodeSet.Contains(x.RfIdCode.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "RF.ID",
                        Message = $"RF.ID '{row.RfIdCode}' sudah terdaftar."
                    });
                }

                // =========================
                // VALIDASI NO.PEKERJA (harus sudah ada di master Pekerja)
                // =========================
                var pekerjas = await _context.Pekerjas
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.NoPekerja))
                        continue; // sudah ditangani oleh ValidateRequiredImportField

                    var match = pekerjas.FirstOrDefault(x =>
                        string.Equals(x.NoPekerja?.Trim(), row.NoPekerja.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "No.Pekerja",
                            Message = $"No.Pekerja '{row.NoPekerja}' tidak ditemukan pada master Pekerja."
                        });
                    }
                }

                // =========================
                // VALIDASI NOPOL (harus sudah ada di master Kendaraan)
                // =========================
                var kendaraans = await _context.Kendaraans
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.Nopol))
                        continue; // sudah ditangani oleh ValidateRequiredImportField

                    var match = kendaraans.FirstOrDefault(x =>
                        string.Equals(x.NomorPolisi?.Trim(), row.Nopol.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Nopol",
                            Message = $"Nopol '{row.Nopol}' tidak ditemukan pada master Kendaraan."
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
                    response.InsertedRfId = 0;

                    return ApiResponse<RfIdImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data RF.ID yang disimpan.");
                }

                // =========================
                // BUILD & SAVE (+ SINKRONISASI Pekerja.RfIds)
                // =========================
                var now = DateTime.UtcNow;
                var newRfIds = new List<RfId>();

                foreach (var row in parsedRows)
                {
                    // No.Pekerja opsional - boleh kosong (kartu belum di-assign)
                    var pekerja = string.IsNullOrWhiteSpace(row.NoPekerja)
                        ? null
                        : pekerjas.FirstOrDefault(x =>
                            string.Equals(x.NoPekerja?.Trim(), row.NoPekerja.Trim(), StringComparison.OrdinalIgnoreCase));

                    var kendaraan = kendaraans.First(x =>
                        string.Equals(x.NomorPolisi?.Trim(), row.Nopol.Trim(), StringComparison.OrdinalIgnoreCase));

                    var rfid = new RfId
                    {
                        RfIdCode = row.RfIdCode.Trim(),
                        PekerjaId = pekerja?.Id,
                        KendaraanId = kendaraan.Id,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newRfIds.Add(rfid);

                    // Setiap RF.ID baru dari bulk upload otomatis nempel ke Pekerja.RfIds (kalau ada pekerja)
                    if (pekerja != null)
                        AddCodeToPekerja(pekerja, rfid.RfIdCode);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new RfIdImportPreviewDto
                        {
                            RfIdCode = rfid.RfIdCode,
                            NoPekerja = pekerja?.NoPekerja ?? string.Empty,
                            NamaPekerja = pekerja?.NamaPekerja ?? string.Empty,
                            Nopol = kendaraan.NomorPolisi
                        });
                    }
                }

                await _context.RfIds.AddRangeAsync(newRfIds);
                await _context.SaveChangesAsync();

                response.InsertedRfId = newRfIds.Count;
                response.SuccessCount = newRfIds.Count;
                response.ErrorCount = 0;

                return ApiResponse<RfIdImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedRfId} RF.ID berhasil diimport.");
            }
            catch (DbUpdateException ex)
            {
                var dbMessage = ex.InnerException?.Message ?? ex.Message;

                if (dbMessage.Contains("IX_RfIds_RfIdCode", StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = 0,
                        Column = "RF.ID",
                        Message = "Terdapat RF.ID yang sudah terdaftar."
                    });

                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedRfId = 0;

                    return ApiResponse<RfIdImportResponse>.SuccessResponse(
                        response,
                        "Import dibatalkan karena terdapat RF.ID duplikat.");
                }

                return ApiResponse<RfIdImportResponse>.ErrorResponse(
                    "ERR-RFID-IMPORT-001", "Terjadi kesalahan saat menyimpan data RF.ID.");
            }
            catch (Exception ex)
            {
                return ApiResponse<RfIdImportResponse>.ErrorResponse(
                    "ERR-RFID-IMPORT-002", $"Gagal mengimport data RF.ID: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("RF.ID");

            var headers = new[] { "RF.ID", "No.Pekerja", "Nopol" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(RfIdImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(RfIdImportHeaderRow, 1, RfIdImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh pengisian (bukan data, tetap harus dihapus/ditimpa user)
            sheet.Cell(2, 1).Value = "1234567891";
            sheet.Cell(2, 2).Value = "19280027";
            sheet.Cell(2, 3).Value = "B1509DYD";
            sheet.Range(2, 1, 2, 3).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, 3).Style.Font.FontColor = XLColor.Gray;

            sheet.Columns().AdjustToContents();

            // Sheet referensi No.Pekerja + Nama Pekerja aktif, supaya user tahu nilai valid
            var pekerjas = await _context.Pekerjas
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.NamaPekerja)
                .ToListAsync();

            var pekerjaRefSheet = workbook.Worksheets.Add("Referensi Pekerja");
            pekerjaRefSheet.Cell(1, 1).Value = "No.Pekerja (Valid)";
            pekerjaRefSheet.Cell(1, 2).Value = "Nama Pekerja";
            pekerjaRefSheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

            for (var i = 0; i < pekerjas.Count; i++)
            {
                pekerjaRefSheet.Cell(i + 2, 1).Value = pekerjas[i].NoPekerja;
                pekerjaRefSheet.Cell(i + 2, 2).Value = pekerjas[i].NamaPekerja;
            }

            pekerjaRefSheet.Columns().AdjustToContents();

            // Sheet referensi Nopol aktif, supaya user tahu nilai valid untuk kolom "Nopol"
            var kendaraans = await _context.Kendaraans
                .Where(k => !k.IsDeleted && k.IsActive)
                .OrderBy(k => k.NomorPolisi)
                .ToListAsync();

            var kendaraanRefSheet = workbook.Worksheets.Add("Referensi Kendaraan");
            kendaraanRefSheet.Cell(1, 1).Value = "Nopol (Valid)";
            kendaraanRefSheet.Cell(1, 1).Style.Font.Bold = true;

            for (var i = 0; i < kendaraans.Count; i++)
                kendaraanRefSheet.Cell(i + 2, 1).Value = kendaraans[i].NomorPolisi;

            kendaraanRefSheet.Columns().AdjustToContents();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            var resultStream = new MemoryStream(bytes);

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_RFID.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = resultStream
            });
        }

        private static Dictionary<string, int> BuildImportHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(headerRow, column).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;

                var normalized = NormalizeImportHeader(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, column);
            }

            return result;
        }

        private static string NormalizeImportHeader(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetImportCellText(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalizedHeader = NormalizeImportHeader(header);
            if (!headerMap.TryGetValue(normalizedHeader, out var column))
                return string.Empty;

            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsRfIdImportRowEmpty(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var rfIdCode = GetImportCellText(sheet, row, headerMap, "RF.ID");
            var noPekerja = GetImportCellText(sheet, row, headerMap, "No.Pekerja");
            var nopol = GetImportCellText(sheet, row, headerMap, "Nopol");
            return string.IsNullOrWhiteSpace(rfIdCode) && string.IsNullOrWhiteSpace(noPekerja) && string.IsNullOrWhiteSpace(nopol);
        }

        private static void ValidateRequiredImportField(List<ImportRowError> errors, int row, string column, string value)
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