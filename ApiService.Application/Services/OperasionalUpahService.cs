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
    public interface IOperasionalUpahService
    {
        Task<ApiResponse<PagedResponse<OperasionalUpahDto>>> GetAllAsync(OperasionalUpahFilterRequest filter);
        Task<ApiResponse<OperasionalUpahDto>> GetByIdAsync(string id);

        /// <summary>
        /// Prefill form Tambah/Edit: kembalikan Jabatan + RfIds + Nopek dari Pekerja yang dipilih.
        /// FE tidak boleh menginput jabatan manual.
        /// </summary>
        Task<ApiResponse<PekerjaLookupDto>> GetPekerjaByIdAsync(string pekerjaId);

        Task<ApiResponse<OperasionalUpahDto>> CreateAsync(CreateOperasionalUpahRequest request, string userId);
        Task<ApiResponse<OperasionalUpahDto>> UpdateAsync(string id, UpdateOperasionalUpahRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);

        /// <summary>
        /// Dashboard "Summary Operasional dan Upah Driver". GrandTotal sudah termasuk
        /// Total BBM dari BbmSubmission yang Status == Approved saja.
        /// </summary>
        Task<ApiResponse<OperasionalUpahSummaryDto>> GetSummaryAsync(OperasionalUpahSummaryRequest filter);

        Task<ApiResponse<OperasionalUpahImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
    }

    public class OperasionalUpahService : IOperasionalUpahService
    {
        private readonly IServiceDbContext _context;

        public OperasionalUpahService(IServiceDbContext context)
        {
            _context = context;
        }

        // BASE QUERY

        private IQueryable<OperasionalUpah> BaseQuery() =>
            _context.OperasionalUpahs
                .Include(o => o.Pekerja)
                    .ThenInclude(p => p!.Jabatan)
                .Include(o => o.Periode)
                .Where(o => !o.IsDeleted);

        // GET ALL

        public async Task<ApiResponse<PagedResponse<OperasionalUpahDto>>> GetAllAsync(OperasionalUpahFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(o =>
                    (o.Pekerja != null && o.Pekerja.NoPekerja.Contains(s)) ||
                    (o.Pekerja != null && o.Pekerja.NamaPekerja.Contains(s)));
            }

            if (!string.IsNullOrEmpty(filter.JabatanId))
                query = query.Where(o => o.Pekerja != null && o.Pekerja.JabatanId == filter.JabatanId);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(o => o.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(o => o.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Ambil total BBM approved untuk semua baris di halaman ini sekaligus (1 query)
            var bbmTotals = await GetApprovedBbmTotalsBatchAsync(
                items.Select(o => (o.PekerjaId, o.PeriodeId)));

            return ApiResponse<PagedResponse<OperasionalUpahDto>>.SuccessResponse(new PagedResponse<OperasionalUpahDto>
            {
                Items = items.Select(o => MapToDto(o, LookupBbmTotal(bbmTotals, o.PekerjaId, o.PeriodeId))).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        // GET BY ID

        public async Task<ApiResponse<OperasionalUpahDto>> GetByIdAsync(string id)
        {
            var item = await BaseQuery().FirstOrDefaultAsync(o => o.Id == id);

            if (item == null)
                return ApiResponse<OperasionalUpahDto>.NotFound("Operasional & Upah tidak ditemukan");

            var bbmTotal = await GetApprovedBbmTotalAsync(item.PekerjaId, item.PeriodeId);
            return ApiResponse<OperasionalUpahDto>.SuccessResponse(MapToDto(item, bbmTotal));
        }

        // PEKERJA LOOKUP (prefill form)

        public async Task<ApiResponse<PekerjaLookupDto>> GetPekerjaByIdAsync(string pekerjaId)
        {
            if (string.IsNullOrWhiteSpace(pekerjaId))
                return ApiResponse<PekerjaLookupDto>.BadRequest("PekerjaId wajib diisi.");

            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == pekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<PekerjaLookupDto>.NotFound("Pekerja tidak ditemukan");

            return ApiResponse<PekerjaLookupDto>.SuccessResponse(new PekerjaLookupDto
            {
                Id = pekerja.Id,
                NoPekerja = pekerja.NoPekerja,
                NamaPekerja = pekerja.NamaPekerja,
                JabatanId = pekerja.JabatanId,
                JabatanName = pekerja.Jabatan?.Name ?? string.Empty,
                RfIds = pekerja.RfIds
            });
        }

        // CREATE

        public async Task<ApiResponse<OperasionalUpahDto>> CreateAsync(CreateOperasionalUpahRequest request, string userId)
        {
            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<OperasionalUpahDto>.ErrorResponse("ERR-OPUPAH-001", "Pekerja tidak ditemukan");

            var periodeExists = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeExists)
                return ApiResponse<OperasionalUpahDto>.ErrorResponse("ERR-OPUPAH-002", "Periode tidak ditemukan");

            // 1 Pekerja hanya boleh 1 record per Periode
            var alreadyExists = await _context.OperasionalUpahs.AnyAsync(o =>
                !o.IsDeleted &&
                o.PekerjaId == request.PekerjaId &&
                o.PeriodeId == request.PeriodeId);

            if (alreadyExists)
                return ApiResponse<OperasionalUpahDto>.Conflict(
                    "Pekerja ini sudah punya data Operasional & Upah di periode tersebut.");

            var item = new OperasionalUpah
            {
                PekerjaId = request.PekerjaId,
                PeriodeId = request.PeriodeId,
                TotalLembur = request.TotalLembur,
                TotalEMoneyMember = request.TotalEMoneyMember,
                DanaOps = request.DanaOps,
                TotalParkir = request.TotalParkir,
                TotalSewaKendaraan = request.TotalSewaKendaraan,
                TotalUpahDriver = request.TotalUpahDriver,
                IsActive = true,
                CreatedBy = userId
            };

            _context.OperasionalUpahs.Add(item);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(o => o.Id == item.Id);
            var bbmTotal = await GetApprovedBbmTotalAsync(created.PekerjaId, created.PeriodeId);
            return ApiResponse<OperasionalUpahDto>.SuccessResponse(MapToDto(created, bbmTotal), "Operasional & Upah berhasil ditambahkan");
        }

        // UPDATE

        public async Task<ApiResponse<OperasionalUpahDto>> UpdateAsync(string id, UpdateOperasionalUpahRequest request, string userId)
        {
            var item = await _context.OperasionalUpahs
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (item == null)
                return ApiResponse<OperasionalUpahDto>.NotFound("Operasional & Upah tidak ditemukan");

            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<OperasionalUpahDto>.ErrorResponse("ERR-OPUPAH-001", "Pekerja tidak ditemukan");

            var periodeExists = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeExists)
                return ApiResponse<OperasionalUpahDto>.ErrorResponse("ERR-OPUPAH-002", "Periode tidak ditemukan");

            // Cek duplikat, exclude diri sendiri
            var alreadyExists = await _context.OperasionalUpahs.AnyAsync(o =>
                !o.IsDeleted &&
                o.PekerjaId == request.PekerjaId &&
                o.PeriodeId == request.PeriodeId &&
                o.Id != id);

            if (alreadyExists)
                return ApiResponse<OperasionalUpahDto>.Conflict(
                    "Pekerja ini sudah punya data Operasional & Upah di periode tersebut.");

            item.PekerjaId = request.PekerjaId;
            item.PeriodeId = request.PeriodeId;
            item.TotalLembur = request.TotalLembur;
            item.TotalEMoneyMember = request.TotalEMoneyMember;
            item.DanaOps = request.DanaOps;
            item.TotalParkir = request.TotalParkir;
            item.TotalSewaKendaraan = request.TotalSewaKendaraan;
            item.TotalUpahDriver = request.TotalUpahDriver;
            item.IsActive = request.IsActive;
            item.ModifiedAt = DateTime.UtcNow;
            item.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(o => o.Id == item.Id);
            var bbmTotal = await GetApprovedBbmTotalAsync(updated.PekerjaId, updated.PeriodeId);
            return ApiResponse<OperasionalUpahDto>.SuccessResponse(MapToDto(updated, bbmTotal), "Operasional & Upah berhasil diperbarui");
        }

        // DELETE

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var item = await _context.OperasionalUpahs
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (item == null)
                return ApiResponse<bool>.NotFound("Operasional & Upah tidak ditemukan");

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            item.DeletedBy = userId;

            await _context.SaveChangesAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Operasional & Upah berhasil dihapus");
        }

        // DASHBOARD SUMMARY
        // Total BBM = hanya BbmSubmission.Status == "Approved". Pending/Rejected
        // tidak masuk GrandTotal, tapi jumlah Pending ditampilkan sebagai info.

        public async Task<ApiResponse<OperasionalUpahSummaryDto>> GetSummaryAsync(OperasionalUpahSummaryRequest filter)
        {
            // Validasi periode dulu (kalau diisi) sebelum query berat
            string? namaPeriode = null;
            if (!string.IsNullOrEmpty(filter.PeriodeId))
            {
                namaPeriode = await _context.Periodes
                    .Where(p => p.Id == filter.PeriodeId && !p.IsDeleted)
                    .Select(p => p.NamaPeriode)
                    .FirstOrDefaultAsync();

                if (namaPeriode == null)
                    return ApiResponse<OperasionalUpahSummaryDto>.NotFound("Periode tidak ditemukan");
            }

            // ── Agregat Operasional & Upah ───────────────────────────────────────
            var opQuery = _context.OperasionalUpahs.Where(o => !o.IsDeleted);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                opQuery = opQuery.Where(o => o.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                opQuery = opQuery.Where(o => o.IsActive == filter.IsActive.Value);

            var jumlahTerisi = await opQuery.CountAsync();
            var totalPekerjaAktif = await _context.Pekerjas.CountAsync(p => !p.IsDeleted && p.IsActive);

            decimal totalLembur = 0, totalEmoney = 0, totalDanaOps = 0,
                    totalParkir = 0, totalSewa = 0, totalUpah = 0;

            if (jumlahTerisi > 0)
            {
                var agg = await opQuery
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        Lembur = g.Sum(o => o.TotalLembur),
                        EMoney = g.Sum(o => o.TotalEMoneyMember),
                        Dana = g.Sum(o => o.DanaOps),
                        Parkir = g.Sum(o => o.TotalParkir),
                        Sewa = g.Sum(o => o.TotalSewaKendaraan),
                        Upah = g.Sum(o => o.TotalUpahDriver)
                    })
                    .FirstAsync();

                totalLembur = agg.Lembur;
                totalEmoney = agg.EMoney;
                totalDanaOps = agg.Dana;
                totalParkir = agg.Parkir;
                totalSewa = agg.Sewa;
                totalUpah = agg.Upah;
            }

            // ── Agregat BBM (dari BbmSubmission, hanya Approved) ────────────────
            var bbmQuery = _context.BbmSubmissions.Where(b => !b.IsDeleted);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                bbmQuery = bbmQuery.Where(b => b.PeriodeId == filter.PeriodeId);

            var totalBbmApproved = await bbmQuery
                .Where(b => b.Status == BbmSubmission.StatusApproved)
                .SumAsync(b => (decimal?)b.NilaiNota) ?? 0m;

            var totalBbmPending = await bbmQuery
                .CountAsync(b => b.Status == BbmSubmission.StatusPending);

            var grandTotal = totalLembur + totalEmoney + totalDanaOps +
                             totalParkir + totalSewa + totalUpah + totalBbmApproved;

            return ApiResponse<OperasionalUpahSummaryDto>.SuccessResponse(new OperasionalUpahSummaryDto
            {
                PeriodeId = filter.PeriodeId,
                NamaPeriode = namaPeriode,
                JumlahTerisi = jumlahTerisi,
                TotalPekerjaAktif = totalPekerjaAktif,
                TotalLembur = totalLembur,
                TotalEMoneyMember = totalEmoney,
                DanaOps = totalDanaOps,
                TotalParkir = totalParkir,
                TotalSewaKendaraan = totalSewa,
                TotalUpahDriver = totalUpah,
                TotalBbmApproved = totalBbmApproved,
                TotalBbmPending = totalBbmPending,
                GrandTotal = grandTotal
            });
        }

        // BULK UPLOAD
        // Kolom: NoPekerja | Periode | TotalLembur | TotalEMoneyMember | DanaOps |
        //        TotalParkir | TotalSewaKendaraan | TotalUpahDriver
        // NoPekerja & Periode wajib; 6 kolom nominal OPSIONAL (kosong = 0).

        private const int HeaderRow = 1;
        private const int DataStartRow = 2;

        private static readonly string[] RequiredHeaders = { "nopekerja", "periode" };

        public async Task<ApiResponse<OperasionalUpahImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId)
        {
            var response = new OperasionalUpahImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<OperasionalUpahImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<OperasionalUpahImportResponse>.BadRequest("Worksheet tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<OperasionalUpahImportResponse>.BadRequest("Worksheet kosong.");

                var headerMap = BuildHeaderMap(sheet, HeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = RequiredHeaders.Where(h => !headerMap.ContainsKey(h)).ToList();
                if (missingHeaders.Count > 0)
                    return ApiResponse<OperasionalUpahImportResponse>.BadRequest(
                        $"Header template tidak lengkap. Kolom hilang: {string.Join(", ", missingHeaders)}.");

                // ── Parse rows ───────────────────────────────────────────────────
                var parsedRows = new List<OperasionalUpahImportRow>();

                for (var row = DataStartRow; row <= lastRow; row++)
                {
                    if (IsRowEmpty(sheet, row, headerMap)) continue;

                    response.TotalRows++;

                    var parsed = new OperasionalUpahImportRow
                    {
                        RowNumber = row,
                        NoPekerja = GetText(sheet, row, headerMap, "NoPekerja"),
                        PeriodeName = GetText(sheet, row, headerMap, "Periode"),
                        TotalLembur = ParseOptionalDecimal(sheet, row, headerMap, "TotalLembur", response.Errors),
                        TotalEMoneyMember = ParseOptionalDecimal(sheet, row, headerMap, "TotalEMoneyMember", response.Errors),
                        DanaOps = ParseOptionalDecimal(sheet, row, headerMap, "DanaOps", response.Errors),
                        TotalParkir = ParseOptionalDecimal(sheet, row, headerMap, "TotalParkir", response.Errors),
                        TotalSewaKendaraan = ParseOptionalDecimal(sheet, row, headerMap, "TotalSewaKendaraan", response.Errors),
                        TotalUpahDriver = ParseOptionalDecimal(sheet, row, headerMap, "TotalUpahDriver", response.Errors)
                    };

                    ValidateRequired(response.Errors, row, "NoPekerja", parsed.NoPekerja);
                    ValidateRequired(response.Errors, row, "Periode", parsed.PeriodeName);

                    parsedRows.Add(parsed);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<OperasionalUpahImportResponse>.BadRequest("Tidak ada data pada file.");

                // ── Duplikat dalam file ──────────────────────────────────────────
                var dupInFile = parsedRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.NoPekerja) && !string.IsNullOrWhiteSpace(r.PeriodeName))
                    .GroupBy(r => $"{r.NoPekerja.Trim().ToLower()}|{r.PeriodeName.Trim().ToLower()}")
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g)
                    .ToList();

                foreach (var dup in dupInFile)
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = dup.RowNumber,
                        Column = "NoPekerja",
                        Message = $"NoPekerja '{dup.NoPekerja}' + Periode '{dup.PeriodeName}' duplikat dalam file."
                    });

                // ── Load master data sekaligus ───────────────────────────────────
                var pekerjas = await _context.Pekerjas.Include(p => p.Jabatan).Where(p => !p.IsDeleted).ToListAsync();
                var periodes = await _context.Periodes.Where(p => !p.IsDeleted).ToListAsync();
                var existingKeys = (await _context.OperasionalUpahs.Where(o => !o.IsDeleted)
                                        .Select(o => new { o.PekerjaId, o.PeriodeId })
                                        .ToListAsync())
                                    .Select(o => $"{o.PekerjaId}|{o.PeriodeId}")
                                    .ToHashSet();

                // ── Validasi per baris ────────────────────────────────────────────
                var contextRows = new List<(OperasionalUpahImportRow Row, Pekerja Pekerja, Periode Periode)>();

                foreach (var r in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(r.NoPekerja) || string.IsNullOrWhiteSpace(r.PeriodeName))
                        continue;

                    var pekerja = pekerjas.FirstOrDefault(p =>
                        string.Equals(p.NoPekerja?.Trim(), r.NoPekerja.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (pekerja == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = r.RowNumber,
                            Column = "NoPekerja",
                            Message = $"NoPekerja '{r.NoPekerja}' tidak ditemukan di master Pekerja."
                        });
                        continue;
                    }

                    var periode = periodes.FirstOrDefault(p =>
                        string.Equals(p.NamaPeriode?.Trim(), r.PeriodeName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (periode == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = r.RowNumber,
                            Column = "Periode",
                            Message = $"Periode '{r.PeriodeName}' tidak ditemukan di master Periode."
                        });
                        continue;
                    }

                    var key = $"{pekerja.Id}|{periode.Id}";
                    if (existingKeys.Contains(key))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = r.RowNumber,
                            Column = "NoPekerja",
                            Message = $"Operasional & Upah untuk '{r.NoPekerja}' periode '{r.PeriodeName}' sudah ada."
                        });
                        continue;
                    }

                    contextRows.Add((r, pekerja, periode));
                }

                // ── All-or-nothing ───────────────────────────────────────────────
                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    return ApiResponse<OperasionalUpahImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data yang disimpan.");
                }

                // ── Build & Save ─────────────────────────────────────────────────
                var now = DateTime.UtcNow;
                var newItems = new List<OperasionalUpah>();

                foreach (var (r, pekerja, periode) in contextRows)
                {
                    var entity = new OperasionalUpah
                    {
                        PekerjaId = pekerja.Id,
                        PeriodeId = periode.Id,
                        TotalLembur = r.TotalLembur,
                        TotalEMoneyMember = r.TotalEMoneyMember,
                        DanaOps = r.DanaOps,
                        TotalParkir = r.TotalParkir,
                        TotalSewaKendaraan = r.TotalSewaKendaraan,
                        TotalUpahDriver = r.TotalUpahDriver,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newItems.Add(entity);

                    if (response.Preview.Count < 10)
                    {
                        var total = r.TotalLembur + r.TotalEMoneyMember + r.DanaOps +
                                    r.TotalParkir + r.TotalSewaKendaraan + r.TotalUpahDriver;

                        response.Preview.Add(new OperasionalUpahImportPreviewDto
                        {
                            NoPekerja = pekerja.NoPekerja,
                            NamaPekerja = pekerja.NamaPekerja,
                            JabatanName = pekerja.Jabatan?.Name ?? string.Empty,
                            NamaPeriode = periode.NamaPeriode,
                            TotalLembur = r.TotalLembur,
                            TotalEMoneyMember = r.TotalEMoneyMember,
                            DanaOps = r.DanaOps,
                            TotalParkir = r.TotalParkir,
                            TotalSewaKendaraan = r.TotalSewaKendaraan,
                            TotalUpahDriver = r.TotalUpahDriver,
                            TotalKeseluruhan = total
                        });
                    }
                }

                await _context.OperasionalUpahs.AddRangeAsync(newItems);
                await _context.SaveChangesAsync();

                response.InsertedOperasionalUpah = newItems.Count;
                response.SuccessCount = newItems.Count;
                response.ErrorCount = 0;

                return ApiResponse<OperasionalUpahImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedOperasionalUpah} data Operasional & Upah berhasil diimport.");
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<OperasionalUpahImportResponse>.ErrorResponse(
                    "ERR-OPUPAH-IMPORT-001",
                    $"Gagal menyimpan data: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<OperasionalUpahImportResponse>.ErrorResponse(
                    "ERR-OPUPAH-IMPORT-002",
                    $"Gagal mengimport: {ex.Message}");
            }
        }

        // TEMPLATE EXCEL

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("OperasionalUpah");

            var headers = new[]
            {
                "NoPekerja", "Periode",
                "TotalLembur", "TotalEMoneyMember", "DanaOps",
                "TotalParkir", "TotalSewaKendaraan", "TotalUpahDriver"
            };

            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(HeaderRow, i + 1).Value = headers[i];

            var hr = sheet.Range(HeaderRow, 1, HeaderRow, headers.Length);
            hr.Style.Font.Bold = true;
            hr.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            hr.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            hr.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh (abu-abu/italic – harus ditimpa user)
            var exRow = new object[] { "19280027", "Januari 2026", 500000, 200000, 1000000, 150000, 3000000, 4500000 };
            for (var i = 0; i < exRow.Length; i++)
                sheet.Cell(DataStartRow, i + 1).Value = XLCellValue.FromObject(exRow[i]);
            sheet.Range(DataStartRow, 1, DataStartRow, headers.Length).Style.Font.Italic = true;
            sheet.Range(DataStartRow, 1, DataStartRow, headers.Length).Style.Font.FontColor = XLColor.Gray;

            // Format kolom nominal sebagai angka bersep-ribuan
            for (var col = 3; col <= headers.Length; col++)
                sheet.Column(col).Style.NumberFormat.Format = "#,##0";

            sheet.Columns().AdjustToContents();

            // Sheet ref Pekerja
            var pekerjas = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.NamaPekerja)
                .ToListAsync();

            var pSheet = workbook.Worksheets.Add("Referensi Pekerja");
            pSheet.Cell(1, 1).Value = "NoPekerja (Valid)";
            pSheet.Cell(1, 2).Value = "Nama Pekerja";
            pSheet.Cell(1, 3).Value = "Jabatan";
            pSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
            for (var i = 0; i < pekerjas.Count; i++)
            {
                pSheet.Cell(i + 2, 1).Value = pekerjas[i].NoPekerja;
                pSheet.Cell(i + 2, 2).Value = pekerjas[i].NamaPekerja;
                pSheet.Cell(i + 2, 3).Value = pekerjas[i].Jabatan?.Name ?? string.Empty;
            }
            pSheet.Columns().AdjustToContents();

            // Sheet ref Periode
            var periodes = await _context.Periodes
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.TanggalAwal)
                .ToListAsync();

            var prSheet = workbook.Worksheets.Add("Referensi Periode");
            prSheet.Cell(1, 1).Value = "NamaPeriode (Valid)";
            prSheet.Cell(1, 2).Value = "Tanggal Awal";
            prSheet.Cell(1, 3).Value = "Tanggal Akhir";
            prSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
            for (var i = 0; i < periodes.Count; i++)
            {
                prSheet.Cell(i + 2, 1).Value = periodes[i].NamaPeriode;
                prSheet.Cell(i + 2, 2).Value = periodes[i].TanggalAwal;
                prSheet.Cell(i + 2, 3).Value = periodes[i].TanggalAkhir;
            }
            prSheet.Columns().AdjustToContents();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_OperasionalUpah.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = new MemoryStream(bytes)
            });
        }

        // BBM HELPERS

        private async Task<decimal> GetApprovedBbmTotalAsync(string pekerjaId, string periodeId)
        {
            // BBM: NilaiNota diakumulasi ke AtasanPekerjaId (VP), bukan PekerjaId driver.
            // Jadi kita cari BbmSubmission di mana AtasanPekerjaId == pekerjaId & PeriodeId cocok.
            return await _context.BbmSubmissions
                .Where(b => !b.IsDeleted &&
                            b.Status == BbmSubmission.StatusApproved &&
                            b.AtasanPekerjaId == pekerjaId &&
                            b.PeriodeId == periodeId)
                .SumAsync(b => (decimal?)b.NilaiNota) ?? 0m;
        }

        private async Task<Dictionary<string, decimal>> GetApprovedBbmTotalsBatchAsync(
            IEnumerable<(string PekerjaId, string PeriodeId)> pairs)
        {
            var list = pairs.Distinct().ToList();
            if (list.Count == 0) return new Dictionary<string, decimal>();

            var pekerjaIds = list.Select(x => x.PekerjaId).Distinct().ToList();
            var periodeIds = list.Select(x => x.PeriodeId).Distinct().ToList();

            var rows = await _context.BbmSubmissions
                .Where(b => !b.IsDeleted &&
                            b.Status == BbmSubmission.StatusApproved &&
                            b.AtasanPekerjaId != null &&
                            pekerjaIds.Contains(b.AtasanPekerjaId!) &&
                            periodeIds.Contains(b.PeriodeId))
                .GroupBy(b => new { b.AtasanPekerjaId, b.PeriodeId })
                .Select(g => new { g.Key.AtasanPekerjaId, g.Key.PeriodeId, Total = g.Sum(b => b.NilaiNota) })
                .ToListAsync();

            return rows.ToDictionary(
                r => $"{r.AtasanPekerjaId}|{r.PeriodeId}",
                r => r.Total);
        }

        private static decimal LookupBbmTotal(Dictionary<string, decimal> dict, string pekerjaId, string periodeId) =>
            dict.TryGetValue($"{pekerjaId}|{periodeId}", out var v) ? v : 0m;

        // EXCEL IMPORT HELPERS

        private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var col = 1; col <= lastColumn; col++)
            {
                var header = sheet.Cell(headerRow, col).GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    var key = Normalize(header);
                    if (!result.ContainsKey(key)) result.Add(key, col);
                }
            }

            return result;
        }

        private static string Normalize(string v) =>
            new(v.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetText(IXLWorksheet sheet, int row, Dictionary<string, int> map, string header)
        {
            if (!map.TryGetValue(Normalize(header), out var col)) return string.Empty;
            return sheet.Cell(row, col).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsRowEmpty(IXLWorksheet sheet, int row, Dictionary<string, int> map)
        {
            var a = GetText(sheet, row, map, "NoPekerja");
            var b = GetText(sheet, row, map, "Periode");
            return string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b);
        }

        private static void ValidateRequired(List<ImportRowError> errors, int row, string col, string val)
        {
            if (string.IsNullOrWhiteSpace(val))
                errors.Add(new ImportRowError
                {
                    RowNumber = row,
                    Column = col,
                    Message = $"Kolom '{col}' wajib diisi."
                });
        }

        /// <summary>
        /// Kolom nominal OPSIONAL: kosong = 0. Kalau diisi tapi tidak bisa di-parse
        /// sebagai angka non-negatif, tambah error dan kembalikan 0.
        /// Mendukung cell numerik (langsung) maupun teks berformat ("999.000" / "1.500,50").
        /// </summary>
        private static decimal ParseOptionalDecimal(
            IXLWorksheet sheet, int row, Dictionary<string, int> map,
            string header, List<ImportRowError> errors)
        {
            if (!map.TryGetValue(Normalize(header), out var col)) return 0m;

            var cell = sheet.Cell(row, col);

            if (cell.DataType == XLDataType.Number)
            {
                var v = cell.GetValue<decimal>();
                if (v < 0)
                {
                    errors.Add(new ImportRowError { RowNumber = row, Column = header, Message = $"'{header}' tidak boleh negatif." });
                    return 0m;
                }
                return v;
            }

            var text = cell.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return 0m; // kosong = 0, bukan error

            var cleaned = new string(text.Where(c => char.IsDigit(c) || c is '.' or ',' or '-').ToArray());
            var lastComma = cleaned.LastIndexOf(',');
            var lastDot = cleaned.LastIndexOf('.');

            var normalized = lastComma > lastDot
                ? cleaned.Replace(".", "").Replace(",", ".")
                : cleaned.Replace(",", "");

            if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = row,
                    Column = header,
                    Message = $"'{header}' tidak valid. Isi angka murni atau kosongkan (default 0)."
                });
                return 0m;
            }

            if (parsed < 0)
            {
                errors.Add(new ImportRowError { RowNumber = row, Column = header, Message = $"'{header}' tidak boleh negatif." });
                return 0m;
            }

            return parsed;
        }

        // MAP TO DTO

        private static OperasionalUpahDto MapToDto(OperasionalUpah o, decimal totalBbmApproved)
        {
            var totalKeseluruhan =
                o.TotalLembur + o.TotalEMoneyMember + o.DanaOps +
                o.TotalParkir + o.TotalSewaKendaraan + o.TotalUpahDriver +
                totalBbmApproved;

            return new OperasionalUpahDto
            {
                Id = o.Id,
                PekerjaId = o.PekerjaId,
                NoPekerja = o.Pekerja?.NoPekerja ?? string.Empty,
                NopekHome = o.Pekerja?.NopekHome ?? string.Empty,
                NopekHost = o.Pekerja?.NopekHost ?? string.Empty,
                NamaPekerja = o.Pekerja?.NamaPekerja ?? string.Empty,
                JabatanId = o.Pekerja?.JabatanId ?? string.Empty,
                JabatanName = o.Pekerja?.Jabatan?.Name ?? string.Empty,
                RfIds = o.Pekerja?.RfIds ?? new List<string>(),
                PeriodeId = o.PeriodeId,
                NamaPeriode = o.Periode?.NamaPeriode ?? string.Empty,
                TotalLembur = o.TotalLembur,
                TotalEMoneyMember = o.TotalEMoneyMember,
                DanaOps = o.DanaOps,
                TotalParkir = o.TotalParkir,
                TotalSewaKendaraan = o.TotalSewaKendaraan,
                TotalUpahDriver = o.TotalUpahDriver,
                TotalBbmApproved = totalBbmApproved,
                TotalKeseluruhan = totalKeseluruhan,
                IsActive = o.IsActive,
                CreatedAt = o.CreatedAt,
                ModifiedAt = o.ModifiedAt
            };
        }
    }
}