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
    public interface ISimCardService
    {
        Task<ApiResponse<PagedResponse<SimCardDto>>> GetAllAsync(SimCardFilterRequest filter);
        Task<ApiResponse<SimCardDto>> GetByIdAsync(string id);
        Task<ApiResponse<SimCardDto>> CreateAsync(CreateSimCardRequest request, string userId);
        Task<ApiResponse<SimCardDto>> UpdateAsync(string id, UpdateSimCardRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        Task<ApiResponse<SimCardSummaryDto>> GetSummaryAsync(SimCardSummaryRequest filter);
        Task<ApiResponse<SimCardImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
    }

    public class SimCardService : ISimCardService
    {
        private readonly IServiceDbContext _context;

        public SimCardService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<SimCard> BaseQuery() =>
            _context.SimCards
                .Include(s => s.Pekerja)
                    .ThenInclude(p => p!.Jabatan)
                .Include(s => s.Periode)
                .Where(s => !s.IsDeleted);

        public async Task<ApiResponse<PagedResponse<SimCardDto>>> GetAllAsync(SimCardFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(x =>
                    (x.Pekerja != null && x.Pekerja.NoPekerja.Contains(s)) ||
                    (x.Pekerja != null && x.Pekerja.NamaPekerja.Contains(s)) ||
                    (x.Pekerja != null && x.Pekerja.NopekHome.Contains(s)) ||
                    (x.Pekerja != null && x.Pekerja.NopekHost.Contains(s)));
            }

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(x => x.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(x => x.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<SimCardDto>>.SuccessResponse(new PagedResponse<SimCardDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<SimCardDto>> GetByIdAsync(string id)
        {
            var simCard = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);

            if (simCard == null)
                return ApiResponse<SimCardDto>.ErrorResponse("ERR-SIMCARD-001", "Sim card not found");

            return ApiResponse<SimCardDto>.SuccessResponse(MapToDto(simCard));
        }

        public async Task<ApiResponse<SimCardDto>> CreateAsync(CreateSimCardRequest request, string userId)
        {
            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<SimCardDto>.ErrorResponse("ERR-SIMCARD-002", "Pekerja tidak ditemukan");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<SimCardDto>.ErrorResponse("ERR-SIMCARD-004", "Periode tidak ditemukan");

            var duplicate = await _context.SimCards
                .AnyAsync(x => !x.IsDeleted && x.PekerjaId == request.PekerjaId && x.PeriodeId == request.PeriodeId);

            if (duplicate)
                return ApiResponse<SimCardDto>.ErrorResponse(
                    "ERR-SIMCARD-003", "SIM card pekerja ini di periode ini sudah terdaftar");

            var simCard = new SimCard
            {
                PekerjaId = request.PekerjaId,
                PeriodeId = request.PeriodeId,
                BiayaSimCard = request.BiayaSimCard,
                IsActive = true,
                CreatedBy = userId
            };

            _context.SimCards.Add(simCard);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(x => x.Id == simCard.Id);
            return ApiResponse<SimCardDto>.SuccessResponse(MapToDto(created), "SIM card berhasil ditambahkan");
        }

        public async Task<ApiResponse<SimCardDto>> UpdateAsync(string id, UpdateSimCardRequest request, string userId)
        {
            var simCard = await _context.SimCards
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (simCard == null)
                return ApiResponse<SimCardDto>.ErrorResponse("ERR-SIMCARD-001", "Sim card not found");

            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<SimCardDto>.ErrorResponse("ERR-SIMCARD-002", "Pekerja tidak ditemukan");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<SimCardDto>.ErrorResponse("ERR-SIMCARD-004", "Periode tidak ditemukan");

            var duplicate = await _context.SimCards
                .AnyAsync(x => !x.IsDeleted && x.Id != id && x.PekerjaId == request.PekerjaId && x.PeriodeId == request.PeriodeId);

            if (duplicate)
                return ApiResponse<SimCardDto>.ErrorResponse(
                    "ERR-SIMCARD-003", "SIM card pekerja ini di periode ini sudah terdaftar");

            simCard.PekerjaId = request.PekerjaId;
            simCard.PeriodeId = request.PeriodeId;
            simCard.BiayaSimCard = request.BiayaSimCard;
            simCard.IsActive = request.IsActive;
            simCard.ModifiedAt = DateTime.UtcNow;
            simCard.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(x => x.Id == simCard.Id);
            return ApiResponse<SimCardDto>.SuccessResponse(MapToDto(updated), "SIM card berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var simCard = await _context.SimCards
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (simCard == null)
                return ApiResponse<bool>.ErrorResponse("ERR-SIMCARD-001", "Sim card not found");

            simCard.IsDeleted = true;
            simCard.DeletedAt = DateTime.UtcNow;
            simCard.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Sim card deleted");
        }

        // =========================================================
        // SUMMARY: total SIM card pekerja (count) + total biaya (sum)
        // =========================================================
        public async Task<ApiResponse<SimCardSummaryDto>> GetSummaryAsync(SimCardSummaryRequest filter)
        {
            string? namaPeriode = null;

            if (!string.IsNullOrEmpty(filter.PeriodeId))
            {
                namaPeriode = await _context.Periodes
                    .Where(p => p.Id == filter.PeriodeId && !p.IsDeleted)
                    .Select(p => p.NamaPeriode)
                    .FirstOrDefaultAsync();

                if (namaPeriode == null)
                    return ApiResponse<SimCardSummaryDto>.NotFound("Periode tidak ditemukan");
            }

            var query = _context.SimCards.Where(x => !x.IsDeleted);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(x => x.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(x => x.IsActive == filter.IsActive.Value);

            var totalSimCardPekerja = await query.CountAsync();
            var totalBiayaSimCard = totalSimCardPekerja == 0 ? 0m : await query.SumAsync(x => x.BiayaSimCard);

            return ApiResponse<SimCardSummaryDto>.SuccessResponse(new SimCardSummaryDto
            {
                PeriodeId = filter.PeriodeId,
                NamaPeriode = namaPeriode,
                TotalSimCardPekerja = totalSimCardPekerja,
                TotalBiayaSimCard = totalBiayaSimCard
            });
        }

        // =========================================================
        // BULK UPLOAD SIM CARD
        // Template kolom: NoPekerja | BiayaSimCard
        // =========================================================

        private const int ImportHeaderRow = 1;
        private const int ImportDataStartRow = 2;

        private static readonly string[] RequiredImportHeaders =
        {
            "nopekerja", "periode", "biayasimcard"
        };

        public async Task<ApiResponse<SimCardImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId)
        {
            var response = new SimCardImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<SimCardImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<SimCardImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<SimCardImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildImportHeaderMap(sheet, ImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = RequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<SimCardImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var parsedRows = new List<SimCardImportRow>();

                for (var row = ImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsImportRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsedRow = new SimCardImportRow
                    {
                        RowNumber = row,
                        NoPekerja = GetImportCellText(sheet, row, headerMap, "NoPekerja"),
                        PeriodeName = GetImportCellText(sheet, row, headerMap, "Periode"),
                        BiayaSimCardText = GetImportCellText(sheet, row, headerMap, "BiayaSimCard")
                    };

                    ValidateRequiredField(response.Errors, row, "NoPekerja", parsedRow.NoPekerja);
                    ValidateRequiredField(response.Errors, row, "Periode", parsedRow.PeriodeName);
                    ValidateRequiredField(response.Errors, row, "BiayaSimCard", parsedRow.BiayaSimCardText);

                    parsedRow.BiayaSimCard = ParseImportDecimal(parsedRow.BiayaSimCardText);
                    if (parsedRow.BiayaSimCard == null || parsedRow.BiayaSimCard <= 0)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "BiayaSimCard",
                            Message = "BiayaSimCard tidak valid. Isi angka murni, contoh: 150000."
                        });
                    }

                    parsedRows.Add(parsedRow);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<SimCardImportResponse>.BadRequest("Tidak ada data SIM Card pada file Excel.");

                var keyInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoPekerja) && !string.IsNullOrWhiteSpace(x.PeriodeName))
                    .GroupBy(x => $"{x.NoPekerja.Trim()}|{x.PeriodeName.Trim()}", StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                            !string.IsNullOrWhiteSpace(x.NoPekerja) &&
                            !string.IsNullOrWhiteSpace(x.PeriodeName) &&
                            keyInFile.Contains($"{x.NoPekerja.Trim()}|{x.PeriodeName.Trim()}")))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "NoPekerja",
                        Message = $"SIM card NoPekerja '{row.NoPekerja}' di periode '{row.PeriodeName}' duplikat di dalam file Excel."
                    });
                }

                var pekerjas = await _context.Pekerjas
                    .Include(p => p.Jabatan)
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();
                var periodes = await _context.Periodes.Where(x => !x.IsDeleted).ToListAsync();
                var existingSimCards = await _context.SimCards.Where(x => !x.IsDeleted).ToListAsync();

                var existingKeys = existingSimCards
                    .Select(x => $"{x.PekerjaId}|{x.PeriodeId}")
                    .ToHashSet();

                var contextRows = new List<SimCardImportContext>();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.NoPekerja))
                        continue; // sudah ditangani required

                    var pekerja = pekerjas.FirstOrDefault(p =>
                        string.Equals(p.NoPekerja?.Trim(), row.NoPekerja.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (pekerja == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NoPekerja",
                            Message = $"NoPekerja '{row.NoPekerja}' tidak ditemukan pada master Pekerja."
                        });
                        continue;
                    }

                    var periode = periodes.FirstOrDefault(p =>
                        string.Equals(p.NamaPeriode?.Trim(), row.PeriodeName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (periode == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Periode",
                            Message = $"Periode '{row.PeriodeName}' tidak ditemukan pada master Periode."
                        });
                        continue;
                    }

                    var existingKey = $"{pekerja.Id}|{periode.Id}";
                    if (existingKeys.Contains(existingKey))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NoPekerja",
                            Message = $"SIM card NoPekerja '{row.NoPekerja}' di periode '{row.PeriodeName}' sudah terdaftar."
                        });
                        continue;
                    }

                    contextRows.Add(new SimCardImportContext
                    {
                        Row = row,
                        Pekerja = pekerja,
                        Periode = periode
                    });
                }

                // —— All-or-nothing ——
                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedSimCard = 0;

                    return ApiResponse<SimCardImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data SIM Card yang disimpan.");
                }

                // —— Build & save ——
                var now = DateTime.UtcNow;
                var newSimCards = new List<SimCard>();

                foreach (var ctx in contextRows)
                {
                    var simCard = new SimCard
                    {
                        PekerjaId = ctx.Pekerja.Id,
                        PeriodeId = ctx.Periode.Id,
                        BiayaSimCard = (decimal)ctx.Row.BiayaSimCard,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newSimCards.Add(simCard);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new SimCardImportPreviewDto
                        {
                            NoPekerja = ctx.Row.NoPekerja.Trim(),
                            NamaPekerja = ctx.Pekerja.NamaPekerja,
                            JabatanName = ctx.Pekerja.Jabatan?.Name ?? string.Empty,
                            NamaPeriode = ctx.Periode.NamaPeriode,
                            BiayaSimCard = simCard.BiayaSimCard
                        });
                    }
                }

                await _context.SimCards.AddRangeAsync(newSimCards);
                await _context.SaveChangesAsync();

                response.InsertedSimCard = newSimCards.Count;
                response.SuccessCount = newSimCards.Count;
                response.ErrorCount = 0;

                return ApiResponse<SimCardImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedSimCard} SIM Card berhasil diimport.");
            }
            catch (Exception ex)
            {
                return ApiResponse<SimCardImportResponse>.ErrorResponse(
                    "ERR-SIMCARD-IMPORT-001", $"Gagal mengimport data SIM Card: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("SimCard");

            var headers = new[] { "NoPekerja", "Periode", "BiayaSimCard" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(ImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(ImportHeaderRow, 1, ImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh (italic abu-abu - harus dihapus/ditimpa user)
            sheet.Cell(2, 1).Value = "19280027";
            sheet.Cell(2, 2).Value = "Januari 2026";
            sheet.Cell(2, 3).Value = "150000";
            sheet.Range(2, 1, 2, headers.Length).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, headers.Length).Style.Font.FontColor = XLColor.Gray;

            sheet.Columns().AdjustToContents();

            // Sheet referensi Pekerja - NoPekerja + NamaPekerja + Jabatan
            var pekerjas = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NamaPekerja)
                .ToListAsync();

            var pekerjaRefSheet = workbook.Worksheets.Add("Referensi Pekerja");
            pekerjaRefSheet.Cell(1, 1).Value = "NoPekerja (Valid)";
            pekerjaRefSheet.Cell(1, 2).Value = "Nama Pekerja";
            pekerjaRefSheet.Cell(1, 3).Value = "Jabatan";
            pekerjaRefSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
            for (var i = 0; i < pekerjas.Count; i++)
            {
                pekerjaRefSheet.Cell(i + 2, 1).Value = pekerjas[i].NoPekerja;
                pekerjaRefSheet.Cell(i + 2, 2).Value = pekerjas[i].NamaPekerja;
                pekerjaRefSheet.Cell(i + 2, 3).Value = pekerjas[i].Jabatan?.Name ?? string.Empty;
            }
            pekerjaRefSheet.Columns().AdjustToContents();

            // Sheet referensi Periode - NamaPeriode + TanggalAwal + TanggalAkhir
            var periodes = await _context.Periodes
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NamaPeriode)
                .ToListAsync();

            var periodeRefSheet = workbook.Worksheets.Add("Referensi Periode");
            periodeRefSheet.Cell(1, 1).Value = "NamaPeriode (Valid)";
            periodeRefSheet.Cell(1, 2).Value = "Tanggal Awal";
            periodeRefSheet.Cell(1, 3).Value = "Tanggal Akhir";
            periodeRefSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
            for (var i = 0; i < periodes.Count; i++)
            {
                periodeRefSheet.Cell(i + 2, 1).Value = periodes[i].NamaPeriode;
                periodeRefSheet.Cell(i + 2, 2).Value = periodes[i].TanggalAwal;
                periodeRefSheet.Cell(i + 2, 3).Value = periodes[i].TanggalAkhir;
            }
            periodeRefSheet.Columns().AdjustToContents();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            var resultStream = new MemoryStream(bytes);

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_SimCard.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = resultStream
            });
        }

        // =========================================================
        // HELPERS (private, khusus import SIM Card)
        // =========================================================

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

        private static string GetImportCellText(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = NormalizeImportHeader(header);
            if (!headerMap.TryGetValue(normalized, out var column))
                return string.Empty;

            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsImportRowEmpty(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var noPekerja = GetImportCellText(sheet, row, headerMap, "NoPekerja");
            return string.IsNullOrWhiteSpace(noPekerja);
        }

        private static void ValidateRequiredField(
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

        /// <summary>
        /// Parse angka murni ("150000", "1.500.000", "1,500,000.50"): abaikan karakter
        /// non-digit; '.' atau ',' pertama dianggap pemisah desimal.
        /// </summary>
        private static decimal? ParseImportDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            long integerPart = 0;
            long fractionPart = 0;
            var fractionDigits = -1;
            var anyDigit = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c >= '0' && c <= '9')
                {
                    if (fractionDigits >= 0)
                    {
                        fractionPart = fractionPart * 10 + (c - '0');
                        fractionDigits++;
                    }
                    else
                    {
                        integerPart = integerPart * 10 + (c - '0');
                    }
                    anyDigit = true;
                }
                else if (c == '.' || c == ',')
                {
                    if (fractionDigits >= 0)
                        break;
                    fractionDigits = 0;
                }
            }

            if (!anyDigit)
                return null;

            var value = (decimal)integerPart;
            if (fractionDigits > 0)
            {
                var divisor = 1m;
                for (var i = 0; i < fractionDigits; i++)
                    divisor *= 10m;

                value += (decimal)fractionPart / divisor;
            }

            return value;
        }

        private sealed class SimCardImportContext
        {
            public SimCardImportRow Row { get; set; } = null!;
            public Pekerja Pekerja { get; set; } = null!;
            public Periode Periode { get; set; } = null!;
        }

        private static SimCardDto MapToDto(SimCard s) => new()
        {
            Id = s.Id,
            PekerjaId = s.PekerjaId,
            NoPekerja = s.Pekerja?.NoPekerja ?? string.Empty,
            NamaPekerja = s.Pekerja?.NamaPekerja ?? string.Empty,
            NopekHome = s.Pekerja?.NopekHome ?? string.Empty,
            NopekHost = s.Pekerja?.NopekHost ?? string.Empty,
            JabatanId = s.Pekerja?.JabatanId ?? string.Empty,
            JabatanName = s.Pekerja?.Jabatan?.Name ?? string.Empty,
            PeriodeId = s.PeriodeId,
            NamaPeriode = s.Periode?.NamaPeriode ?? string.Empty,
            BiayaSimCard = s.BiayaSimCard,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            ModifiedAt = s.ModifiedAt
        };
    }
}