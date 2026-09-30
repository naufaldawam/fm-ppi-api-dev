using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface IOperasionalTagihanBbmBulkUploadService
    {
        Task<ApiResponse<PagedResponse<OperasionalTagihanBbmDto>>> GetAllAsync(OperasionalTagihanBbmFilterRequest filter);
        Task<ApiResponse<OperasionalTagihanBbmDto>> GetByIdAsync(string id);
        Task<ApiResponse<OperasionalTagihanBbmImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
    }

    public class OperasionalTagihanBbmBulkUploadService(IServiceDbContext context) : IOperasionalTagihanBbmBulkUploadService
    {
        private readonly IServiceDbContext _context = context;

        private const int HeaderRow = 1;
        private const int DataStartRow = 2;

        private static readonly string[] RequiredHeaders =
        {
            "periode",
            "tanggalritel",
            "nomorreferensiritel",
            "nopekerjaritel",
            "nomorpolisiRitel".ToLowerInvariant(),
            "jumlahbbmritel",
            "nilairitel"
        };

        public async Task<ApiResponse<PagedResponse<OperasionalTagihanBbmDto>>> GetAllAsync(
            OperasionalTagihanBbmFilterRequest filter)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 10 : Math.Min(filter.PageSize, 100);

            var query = _context.OperasionalTagihanBbmRekonsiliasis
                .Include(x => x.Periode)
                .Include(x => x.Driver)
                .Include(x => x.Kendaraan)
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();
                query = query.Where(x =>
                    (x.NoPekerjaRitel != null && x.NoPekerjaRitel.Contains(search)) ||
                    x.NomorPolisiRitel.Contains(search) ||
                    (x.NamaRitel != null && x.NamaRitel.Contains(search)) ||
                    (x.Driver != null && x.Driver.NamaDriver.Contains(search)) ||
                    (x.Driver != null && x.Driver.NoPekerja.Contains(search)) ||
                    (x.Kendaraan != null && x.Kendaraan.NomorPolisi.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filter.PeriodeId))
                query = query.Where(x => x.PeriodeId == filter.PeriodeId);

            if (!string.IsNullOrWhiteSpace(filter.StatusMatching))
                query = query.Where(x => x.StatusMatching == filter.StatusMatching);

            if (filter.TanggalFrom.HasValue)
            {
                var from = filter.TanggalFrom.Value.Date;
                query = query.Where(x => x.TanggalRitel >= from);
            }

            if (filter.TanggalTo.HasValue)
            {
                var to = filter.TanggalTo.Value.Date.AddDays(1);
                query = query.Where(x => x.TanggalRitel < to);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.TanggalRitel)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<OperasionalTagihanBbmDto>>.SuccessResponse(
                new PagedResponse<OperasionalTagihanBbmDto>
                {
                    Items = items.Select(MapToDto).ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                });
        }

        public async Task<ApiResponse<OperasionalTagihanBbmDto>> GetByIdAsync(string id)
        {
            var item = await _context.OperasionalTagihanBbmRekonsiliasis
                .Include(x => x.Periode)
                .Include(x => x.Driver)
                .Include(x => x.Kendaraan)
                .Include(x => x.BbmSubmission)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (item == null)
                return ApiResponse<OperasionalTagihanBbmDto>.NotFound("Data rekonsiliasi BBM tidak ditemukan.");

            return ApiResponse<OperasionalTagihanBbmDto>.SuccessResponse(MapToDto(item));
        }

        public async Task<ApiResponse<OperasionalTagihanBbmImportResponse>> ImportFromExcelAsync(
            Stream fileStream,
            string userId)
        {
            var response = new OperasionalTagihanBbmImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();
                if (usedRange == null)
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildHeaderMap(sheet, HeaderRow);
                var missingHeaders = RequiredHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest(
                        $"Header template tidak lengkap. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var lastRow = usedRange.LastRow().RowNumber();
                var parsedRows = new List<OperasionalTagihanBbmImportRow>();

                for (var row = DataStartRow; row <= lastRow; row++)
                {
                    if (IsRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var item = new OperasionalTagihanBbmImportRow
                    {
                        RowNumber = row,
                        PeriodeName = GetText(sheet, row, headerMap, "Periode"),
                        TanggalRitelText = GetText(sheet, row, headerMap, "TanggalRitel"),
                        NomorReferensiRitel = GetText(sheet, row, headerMap, "NomorReferensiRitel"),
                        NoPekerjaRitel = GetText(sheet, row, headerMap, "NoPekerjaRitel"),
                        NomorPolisiRitel = GetText(sheet, row, headerMap, "NomorPolisiRitel"),
                        NamaRitel = GetText(sheet, row, headerMap, "NamaRitel"),
                        JumlahBbmRitelText = GetText(sheet, row, headerMap, "JumlahBbmRitel"),
                        NilaiRitelText = GetText(sheet, row, headerMap, "NilaiRitel")
                    };

                    item.TanggalRitel = ParseDate(item.TanggalRitelText);
                    item.JumlahBbmRitel = ParseDecimal(item.JumlahBbmRitelText);
                    item.NilaiRitel = ParseDecimal(item.NilaiRitelText);

                    ValidateRequired(response.Errors, row, "Periode", item.PeriodeName);
                    ValidateRequired(response.Errors, row, "TanggalRitel", item.TanggalRitelText);
                    ValidateRequired(response.Errors, row, "NoPekerjaRitel", item.NoPekerjaRitel);
                    ValidateRequired(response.Errors, row, "NomorPolisiRitel", item.NomorPolisiRitel);
                    ValidateRequired(response.Errors, row, "JumlahBbmRitel", item.JumlahBbmRitelText);
                    ValidateRequired(response.Errors, row, "NilaiRitel", item.NilaiRitelText);

                    if (!string.IsNullOrWhiteSpace(item.TanggalRitelText) && !item.TanggalRitel.HasValue)
                        AddError(response.Errors, row, "TanggalRitel", "Format tanggal tidak valid.");

                    if (!string.IsNullOrWhiteSpace(item.JumlahBbmRitelText) && !item.JumlahBbmRitel.HasValue)
                        AddError(response.Errors, row, "JumlahBbmRitel", "Format angka tidak valid.");

                    if (!string.IsNullOrWhiteSpace(item.NilaiRitelText) && !item.NilaiRitel.HasValue)
                        AddError(response.Errors, row, "NilaiRitel", "Format angka tidak valid.");

                    if (item.JumlahBbmRitel.HasValue && item.JumlahBbmRitel.Value < 0)
                        AddError(response.Errors, row, "JumlahBbmRitel", "Jumlah BBM tidak boleh negatif.");

                    if (item.NilaiRitel.HasValue && item.NilaiRitel.Value < 0)
                        AddError(response.Errors, row, "NilaiRitel", "Nilai Ritel tidak boleh negatif.");

                    parsedRows.Add(item);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest("Tidak ada data pada file.");

                var dateNow = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
 
                var duplicateRows = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoPekerjaRitel) && x.TanggalRitel.HasValue)
                    .GroupBy(x => string.Join("|",
                        x.PeriodeName.Trim().ToLowerInvariant(),
                        x.NoPekerjaRitel.Trim().ToLowerInvariant(),
                        x.NomorPolisiRitel.Trim().ToLowerInvariant(),
                        x.TanggalRitel.HasValue? x.TanggalRitel.Value.Date.ToString("yyyy-MM-dd") : dateNow,
                        x.NomorReferensiRitel.Trim().ToLowerInvariant()))
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g)
                    .ToList();

                foreach (var duplicate in duplicateRows)
                {
                    AddError(
                        response.Errors,
                        duplicate.RowNumber,
                        "NoPekerjaRitel",
                        "Data Ritel duplikat di dalam file berdasarkan periode, pekerja, kendaraan, tanggal, dan nomor referensi.");
                }

                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data yang disimpan.");
                }

                var periodes = await _context.Periodes
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                var drivers = await _context.Drivers
                    .Where(x => !x.IsDeleted && x.IsActive)
                    .ToListAsync();

                var kendaraans = await _context.Kendaraans
                    .Where(x => !x.IsDeleted && x.IsActive)
                    .ToListAsync();

                var existingReconciliationKeys = (await _context.OperasionalTagihanBbmRekonsiliasis
                    .Where(x => !x.IsDeleted)
                    .Select(x => new
                    {
                        x.PeriodeId,
                        x.TanggalRitel,
                        x.NoPekerjaRitel,
                        x.NomorPolisiRitel,
                        x.NomorReferensiRitel
                    })
                    .ToListAsync())
                    .Select(x => BuildExistingKey(
                        x.PeriodeId,
                        x.TanggalRitel,
                        x.NoPekerjaRitel,
                        x.NomorPolisiRitel,
                        x.NomorReferensiRitel))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var newItems = new List<OperasionalTagihanBbmRekonsiliasi>();

                foreach (var row in parsedRows)
                {
                    var periode = periodes.FirstOrDefault(x =>
                        string.Equals(x.NamaPeriode?.Trim(), row.PeriodeName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (periode == null)
                    {
                        AddError(response.Errors, row.RowNumber, "Periode",
                            $"Periode '{row.PeriodeName}' tidak ditemukan di master Periode.");
                        continue;
                    }

                    var driver = drivers.FirstOrDefault(x =>
                        string.Equals(x.NoPekerja?.Trim(), row.NoPekerjaRitel.Trim(), StringComparison.OrdinalIgnoreCase));

                    var kendaraan = kendaraans.FirstOrDefault(x =>
                        string.Equals(x.NomorPolisi?.Trim(), row.NomorPolisiRitel.Trim(), StringComparison.OrdinalIgnoreCase));

                    var existingKey = BuildExistingKey(
                        periode.Id,
                        row.TanggalRitel!.Value,
                        row.NoPekerjaRitel,
                        row.NomorPolisiRitel,
                        row.NomorReferensiRitel);

                    if (existingReconciliationKeys.Contains(existingKey))
                    {
                        AddError(response.Errors, row.RowNumber, "NoPekerjaRitel",
                            "Data Ritel dengan kombinasi periode, pekerja, kendaraan, tanggal, dan nomor referensi sudah pernah diupload.");
                        continue;
                    }

                    BbmSubmission? exactSubmission = null;
                    BbmSubmission? sameDriverDateSubmission = null;

                    if (driver != null)
                    {
                        exactSubmission = await _context.BbmSubmissions
                            .Include(x => x.Driver)
                            .Include(x => x.Kendaraan)
                            .FirstOrDefaultAsync(x =>
                                !x.IsDeleted &&
                                x.DriverId == driver.Id &&
                                x.PeriodeId == periode.Id &&
                               x.KendaraanId == (kendaraan != null ? kendaraan.Id : null) &&
                                x.TanggalPenggunaan.Date == row.TanggalRitel.Value.Date &&
                                x.Status != BbmSubmission.StatusRejected);

                        sameDriverDateSubmission = await _context.BbmSubmissions
                            .Include(x => x.Driver)
                            .Include(x => x.Kendaraan)
                            .Where(x =>
                                !x.IsDeleted &&
                                x.DriverId == driver.Id &&
                                x.PeriodeId == periode.Id &&
                                x.TanggalPenggunaan.Date == row.TanggalRitel.Value.Date &&
                                x.Status != BbmSubmission.StatusRejected)
                            .OrderByDescending(x => x.CreatedAt)
                            .FirstOrDefaultAsync();
                    }

                    var submissionForComparison = exactSubmission ?? sameDriverDateSubmission;
                    var status = OperasionalTagihanBbmRekonsiliasi.StatusNotMatched;
                    string? mismatchReason = null;
                    DateTime? matchedAt = null;

                    if (exactSubmission != null)
                    {
                        var reasons = new List<string>();

                        if (exactSubmission.JumlahPenggunaanBbm != row.JumlahBbmRitel!.Value)
                            reasons.Add("Jumlah BBM berbeda.");

                        if (exactSubmission.NilaiNota != row.NilaiRitel!.Value)
                            reasons.Add("Nilai nota berbeda.");

                        status = reasons.Count == 0
                            ? OperasionalTagihanBbmRekonsiliasi.StatusMatched
                            : OperasionalTagihanBbmRekonsiliasi.StatusMismatch;

                        mismatchReason = reasons.Count == 0 ? null : string.Join(" ", reasons);
                        matchedAt = DateTime.UtcNow;
                    }
                    else if (sameDriverDateSubmission != null)
                    {
                        status = OperasionalTagihanBbmRekonsiliasi.StatusMismatch;
                        mismatchReason = kendaraan == null
                            ? "Kendaraan Ritel tidak ditemukan di master Kendaraan."
                            : "Kendaraan pada data Ritel berbeda dengan submission Driver.";
                        matchedAt = DateTime.UtcNow;
                    }
                    else if (driver == null)
                    {
                        mismatchReason = "Driver dengan NoPekerja Ritel tidak ditemukan atau tidak aktif.";
                    }
                    else if (kendaraan == null)
                    {
                        mismatchReason = "Nomor polisi Ritel tidak ditemukan atau tidak aktif di master Kendaraan.";
                    }
                    else
                    {
                        mismatchReason = "Submission Driver yang sesuai tidak ditemukan untuk periode, driver, kendaraan, dan tanggal tersebut.";
                    }

                    var entity = new OperasionalTagihanBbmRekonsiliasi
                    {
                        PeriodeId = periode.Id,
                        BbmSubmissionId = submissionForComparison?.Id,
                        TanggalRitel = row.TanggalRitel.Value,
                        NomorReferensiRitel = NullIfWhiteSpace(row.NomorReferensiRitel),
                        NoPekerjaRitel = NullIfWhiteSpace(row.NoPekerjaRitel),
                        NomorPolisiRitel = row.NomorPolisiRitel.Trim(),
                        NamaRitel = NullIfWhiteSpace(row.NamaRitel),
                        JumlahBbmRitel = row.JumlahBbmRitel!.Value,
                        NilaiRitel = row.NilaiRitel!.Value,
                        DriverId = driver?.Id,
                        KendaraanId = kendaraan?.Id,
                        JumlahBbmDriver = submissionForComparison?.JumlahPenggunaanBbm,
                        NilaiNotaDriver = submissionForComparison?.NilaiNota,
                        TanggalDriver = submissionForComparison?.TanggalPenggunaan,
                        StatusMatching = status,
                        MismatchReason = mismatchReason,
                        SourceFileName = null,
                        SourceRowNumber = row.RowNumber,
                        MatchedAt = matchedAt,
                        IsActive = true,
                        CreatedBy = userId
                    };

                    newItems.Add(entity);

                    if (response.Preview.Count < 20)
                    {
                        response.Preview.Add(new OperasionalTagihanBbmImportPreviewDto
                        {
                            NoPekerjaRitel = row.NoPekerjaRitel,
                            NamaDriver = driver?.NamaDriver,
                            NomorPolisiRitel = row.NomorPolisiRitel,
                            NomorPolisiDriver = submissionForComparison?.Kendaraan?.NomorPolisi,
                            NamaPeriode = periode.NamaPeriode,
                            TanggalRitel = row.TanggalRitel.Value,
                            JumlahBbmRitel = row.JumlahBbmRitel.Value,
                            JumlahBbmDriver = submissionForComparison?.JumlahPenggunaanBbm,
                            NilaiRitel = row.NilaiRitel.Value,
                            NilaiNotaDriver = submissionForComparison?.NilaiNota,
                            StatusMatching = status,
                            MismatchReason = mismatchReason
                        });
                    }
                }

                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data yang disimpan.");
                }

                await _context.OperasionalTagihanBbmRekonsiliasis.AddRangeAsync(newItems);
                await _context.SaveChangesAsync();

                response.InsertedOperasionalTagihanBbmRekonsiliasi = newItems.Count;
                response.SuccessCount = newItems.Count;
                response.ErrorCount = 0;
                response.MatchedCount = newItems.Count(x => x.StatusMatching == OperasionalTagihanBbmRekonsiliasi.StatusMatched);
                response.MismatchCount = newItems.Count(x => x.StatusMatching == OperasionalTagihanBbmRekonsiliasi.StatusMismatch);
                response.NotMatchedCount = newItems.Count(x => x.StatusMatching == OperasionalTagihanBbmRekonsiliasi.StatusNotMatched);

                return ApiResponse<OperasionalTagihanBbmImportResponse>.SuccessResponse(
                    response,
                    $"{newItems.Count} data rekonsiliasi BBM berhasil diimport.");
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<OperasionalTagihanBbmImportResponse>.ErrorResponse(
                    "ERR-BBM-RECON-IMPORT-001",
                    $"Gagal menyimpan data rekonsiliasi BBM: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<OperasionalTagihanBbmImportResponse>.ErrorResponse(
                    "ERR-BBM-RECON-IMPORT-002",
                    $"Gagal mengimport data rekonsiliasi BBM: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("OperasionalTagihanBbmRekonsiliasi");

            var headers = new[]
            {
                "Periode",
                "TanggalRitel",
                "NomorReferensiRitel",
                "NoPekerjaRitel",
                "NomorPolisiRitel",
                "NamaRitel",
                "JumlahBbmRitel",
                "NilaiRitel"
            };

            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(HeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(HeaderRow, 1, HeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            sheet.Cell(DataStartRow, 1).Value = "Januari 2026";
            sheet.Cell(DataStartRow, 2).Value = "2026-09-11";
            sheet.Cell(DataStartRow, 3).Value = "REF-001";
            sheet.Cell(DataStartRow, 4).Value = "DRV001";
            sheet.Cell(DataStartRow, 5).Value = "B 1234 XYZ";
            sheet.Cell(DataStartRow, 6).Value = "Contoh Ritel";
            sheet.Cell(DataStartRow, 7).Value = "50";
            sheet.Cell(DataStartRow, 8).Value = "750000";
            sheet.Range(DataStartRow, 1, DataStartRow, headers.Length).Style.Font.Italic = true;
            sheet.Range(DataStartRow, 1, DataStartRow, headers.Length).Style.Font.FontColor = XLColor.Gray;

            sheet.Columns().AdjustToContents();

            var periodeSheet = workbook.Worksheets.Add("Referensi Periode");
            periodeSheet.Cell(1, 1).Value = "NamaPeriode (Valid)";
            periodeSheet.Cell(1, 2).Value = "Tanggal Awal";
            periodeSheet.Cell(1, 3).Value = "Tanggal Akhir";
            periodeSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;

            var periodes = await _context.Periodes
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NamaPeriode)
                .ToListAsync();

            for (var i = 0; i < periodes.Count; i++)
            {
                periodeSheet.Cell(i + 2, 1).Value = periodes[i].NamaPeriode;
                periodeSheet.Cell(i + 2, 2).Value = periodes[i].TanggalAwal;
                periodeSheet.Cell(i + 2, 3).Value = periodes[i].TanggalAkhir;
            }

            var driverSheet = workbook.Worksheets.Add("Referensi Driver");
            driverSheet.Cell(1, 1).Value = "NoPekerja";
            driverSheet.Cell(1, 2).Value = "NamaDriver";
            driverSheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

            var drivers = await _context.Drivers
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NoPekerja)
                .ToListAsync();

            for (var i = 0; i < drivers.Count; i++)
            {
                driverSheet.Cell(i + 2, 1).Value = drivers[i].NoPekerja;
                driverSheet.Cell(i + 2, 2).Value = drivers[i].NamaDriver;
            }

            var kendaraanSheet = workbook.Worksheets.Add("Referensi Kendaraan");
            kendaraanSheet.Cell(1, 1).Value = "NomorPolisi";
            kendaraanSheet.Cell(1, 2).Value = "Merek";
            kendaraanSheet.Range(1, 1, 1, 2).Style.Font.Bold = true;

            var kendaraans = await _context.Kendaraans
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NomorPolisi)
                .ToListAsync();

            for (var i = 0; i < kendaraans.Count; i++)
            {
                kendaraanSheet.Cell(i + 2, 1).Value = kendaraans[i].NomorPolisi;
                kendaraanSheet.Cell(i + 2, 2).Value = kendaraans[i].Merek;
            }

            periodeSheet.Columns().AdjustToContents();
            driverSheet.Columns().AdjustToContents();
            kendaraanSheet.Columns().AdjustToContents();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_Bbm_Reconciliation.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = new MemoryStream(bytes)
            });
        }

        private static OperasionalTagihanBbmDto MapToDto(OperasionalTagihanBbmRekonsiliasi entity) => new()
        {
            Id = entity.Id,
            PeriodeId = entity.PeriodeId,
            NamaPeriode = entity.Periode?.NamaPeriode ?? string.Empty,
            BbmSubmissionId = entity.BbmSubmissionId,
            TanggalRitel = entity.TanggalRitel,
            NomorReferensiRitel = entity.NomorReferensiRitel,
            NoPekerjaRitel = entity.NoPekerjaRitel,
            NomorPolisiRitel = entity.NomorPolisiRitel,
            NamaRitel = entity.NamaRitel,
            JumlahBbmRitel = entity.JumlahBbmRitel,
            NilaiRitel = entity.NilaiRitel,
            DriverId = entity.DriverId,
            NoPekerjaDriver = entity.Driver?.NoPekerja,
            NamaDriver = entity.Driver?.NamaDriver,
            KendaraanId = entity.KendaraanId,
            NomorPolisiDriver = entity.Kendaraan?.NomorPolisi,
            JumlahBbmDriver = entity.JumlahBbmDriver,
            NilaiNotaDriver = entity.NilaiNotaDriver,
            TanggalDriver = entity.TanggalDriver,
            StatusMatching = entity.StatusMatching,
            MismatchReason = entity.MismatchReason,
            SourceFileName = entity.SourceFileName,
            SourceRowNumber = entity.SourceRowNumber,
            MatchedAt = entity.MatchedAt,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };

        private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(headerRow, column).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;

                var normalized = Normalize(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, column);
            }

            return result;
        }

        private static string GetText(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = Normalize(header);
            return headerMap.TryGetValue(normalized, out var column)
                ? sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static bool IsRowEmpty(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            return string.IsNullOrWhiteSpace(GetText(sheet, row, headerMap, "Periode"));
        }

        private static string Normalize(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static DateTime? ParseDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date.Date;

            if (DateTime.TryParse(text, new CultureInfo("id-ID"), DateTimeStyles.None, out date))
                return date.Date;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial) && serial > 0)
            {
                try { return DateTime.FromOADate(serial).Date; }
                catch { }
            }

            return null;
        }

        private static decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var cleaned = text.Trim().Replace("Rp", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "");

            if (decimal.TryParse(cleaned, NumberStyles.Any, new CultureInfo("id-ID"), out var idValue))
                return idValue;

            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariantValue))
                return invariantValue;

            return null;
        }

        private static void ValidateRequired(List<ImportRowError> errors, int row, string column, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                AddError(errors, row, column, $"Data wajib '{column}' kosong.");
        }

        private static void AddError(List<ImportRowError> errors, int row, string column, string message)
        {
            errors.Add(new ImportRowError
            {
                RowNumber = row,
                Column = column,
                Message = message
            });
        }

        private static string BuildExistingKey(
            string periodeId,
            DateTime tanggal,
            string? noPekerja,
            string nomorPolisi,
            string? nomorReferensi)
        {
            return string.Join("|",
                periodeId,
                tanggal.Date.ToString("yyyy-MM-dd"),
                (noPekerja ?? string.Empty).Trim().ToLowerInvariant(),
                nomorPolisi.Trim().ToLowerInvariant(),
                (nomorReferensi ?? string.Empty).Trim().ToLowerInvariant());
        }

        private static string? NullIfWhiteSpace(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
