using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;
using ApiService.Application.Configurations;
using System.IO;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ApiService.Application.Services
{
    public interface IDataKecelakaanService
    {
        Task<ApiResponse<PagedResponse<DataKecelakaanDto>>> GetAllAsync(DataKecelakaanFilterRequest filter);
        Task<ApiResponse<DataKecelakaanDto>> GetByIdAsync(string id);
        /// <summary>Create dari satu form multipart: fields + photos (foto bukti 0..N).</summary>
        Task<ApiResponse<DataKecelakaanDto>> CreateAsync(
            CreateDataKecelakaanRequest request, List<IFormFile>? photos, string userId);
        Task<ApiResponse<DataKecelakaanDto>> UpdateAsync(
            string id, UpdateDataKecelakaanRequest request, List<IFormFile>? photos, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        /// <summary>Tambah 1 foto bukti ke laporan (SortOrder max+1).</summary>
        Task<ApiResponse<EvidenceKecelakaanDto>> UploadEvidenceAsync(string dataKecelakaanId, IFormFile photo, string userId);
        /// <summary>Soft delete 1 foto bukti.</summary>
        Task<ApiResponse<bool>> DeleteEvidenceAsync(string evidenceId, string userId);
        /// <summary>Ambil bytes foto bukti untuk streaming ke FE.</summary>
        Task<ApiResponse<EvidenceFileDto>> GetEvidenceImageAsync(string evidenceId);
        /// <summary>Generate PDF laporan kecelakaan (QuestPDF).</summary>
        Task<ApiResponse<FileResult>> GeneratePdfAsync(string id);
        Task<ApiResponse<DataKecelakaanSummaryDto>> GetSummaryAsync(DataKecelakaanSummaryRequest filter);
    }

    public class DataKecelakaanService : IDataKecelakaanService
    {
        private readonly IServiceDbContext _context;
        private readonly IFileService _fileService;
        private readonly StorageConfig _storage;

        public DataKecelakaanService(
            IServiceDbContext context,
            IFileService fileService,
            IOptions<StorageConfig> storage)
        {
            _context = context;
            _fileService = fileService;
            _storage = storage.Value;
        }

        private IQueryable<DataKecelakaan> BaseQuery() =>
            _context.DataKecelakaans
                .Include(d => d.Kategori)
                .Include(d => d.Periode)
                .Include(d => d.Driver)
                .Include(d => d.Pejabat)
                .Where(d => !d.IsDeleted);

        public async Task<ApiResponse<PagedResponse<DataKecelakaanDto>>> GetAllAsync(DataKecelakaanFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(d =>
                    d.Nomor.Contains(s) ||
                    d.Judul.Contains(s) ||
                    d.Alamat.Contains(s));
            }

            if (!string.IsNullOrEmpty(filter.KategoriId))
                query = query.Where(d => d.KategoriId == filter.KategoriId);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(d => d.PeriodeId == filter.PeriodeId);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(d => d.Status == filter.Status);

            if (filter.TanggalFrom.HasValue)
                query = query.Where(d => d.TanggalKejadian >= ((DateTime)filter.TanggalFrom).Date);

            if (filter.TanggalTo.HasValue)
                query = query.Where(d => d.TanggalKejadian <= ((DateTime)filter.TanggalTo).Date);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(d => d.TanggalKejadian)
                .ThenByDescending(d => d.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var ids = items.Select(d => d.Id).ToList();
            var evidences = await _context.EvidenceKecelakaans
                .Where(e => !e.IsDeleted && ids.Contains(e.DataKecelakaanId))
                .OrderBy(e => e.SortOrder)
                .ToListAsync();

            var evidenceByReport = evidences
                .GroupBy(e => e.DataKecelakaanId)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.SortOrder).ToList());

            return ApiResponse<PagedResponse<DataKecelakaanDto>>.SuccessResponse(new PagedResponse<DataKecelakaanDto>
            {
                Items = items.Select(d =>
                {
                    var list = new List<EvidenceKecelakaan>();
                    if (evidenceByReport.ContainsKey(d.Id))
                        list = evidenceByReport[d.Id];
                    return MapToDto(d, list);
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<DataKecelakaanDto>> GetByIdAsync(string id)
        {
            var dataKecelakaan = await BaseQuery().FirstOrDefaultAsync(d => d.Id == id);

            if (dataKecelakaan == null)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-001", "Data kecelakaan not found");

            var evidences = await _context.EvidenceKecelakaans
                .Where(e => !e.IsDeleted && e.DataKecelakaanId == id)
                .OrderBy(e => e.SortOrder)
                .ToListAsync();

            return ApiResponse<DataKecelakaanDto>.SuccessResponse(MapToDto(dataKecelakaan, evidences));
        }

        public async Task<ApiResponse<DataKecelakaanDto>> CreateAsync(
            CreateDataKecelakaanRequest request, List<IFormFile>? photos, string userId)
        {
            var kategoriOk = await _context.KategoriKecelakaans
                .AnyAsync(k => k.Id == request.KategoriId && !k.IsDeleted);
            if (!kategoriOk)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-002", "Kategori tidak ditemukan");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);
            if (!periodeOk)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-010", "Periode tidak ditemukan");

            if (!string.IsNullOrEmpty(request.DriverId))
            {
                var driverOk = await _context.Drivers.AnyAsync(d => d.Id == request.DriverId && !d.IsDeleted);
                if (!driverOk)
                    return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-003", "Driver tidak ditemukan");
            }

            if (!string.IsNullOrEmpty(request.PejabatId))
            {
                var pejabatOk = await _context.Pekerjas.AnyAsync(p => p.Id == request.PejabatId && !p.IsDeleted);
                if (!pejabatOk)
                    return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-004", "Pejabat tidak ditemukan");
            }

            var nomor = request.Nomor.Trim();
            var nomorExists = await _context.DataKecelakaans
                .AnyAsync(d => d.Nomor == nomor && !d.IsDeleted);
            if (nomorExists)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-009", "Nomor laporan sudah ada");

            var tanggal = DateTimeUtil.ParseDateOnly(request.TanggalKejadian);
            if (tanggal == null)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse(
                    "ERR-DATAKECELAKAAN-005", "Tanggal kejadian tidak valid. Format: yyyy-MM-dd.");

            var dataKecelakaan = new DataKecelakaan
            {
                Nomor = nomor,
                Judul = request.Judul.Trim(),
                KategoriId = request.KategoriId,
                PeriodeId = request.PeriodeId,
                TanggalKejadian = (DateTime)tanggal,
                WaktuKejadian = request.WaktuKejadian.Trim(),
                Dampak = request.Dampak.Trim(),
                DriverId = string.IsNullOrWhiteSpace(request.DriverId) ? null : request.DriverId,
                PejabatId = string.IsNullOrWhiteSpace(request.PejabatId) ? null : request.PejabatId,
                Alamat = request.Alamat.Trim(),
                DetailKejadian = request.DetailKejadian.Trim(),
                PenyebabKejadian = request.PenyebabKejadian.Trim(),
                BagaimanaTerjadinya = request.BagaimanaTerjadinya.Trim(),
                AkarPermasalahan = request.AkarPermasalahan.Trim(),
                TindakanSegara = request.TindakanSegara.Trim(),
                TindakanPerbaikan = request.TindakanPerbaikan.Trim(),
                // Submit langsung: tanpa workflow draft/publish
                Status = DataKecelakaan.StatusPublished,
                Revisi = 0,
                IsActive = true,
                CreatedBy = userId
            };

            var evidences = new List<EvidenceKecelakaan>();
            if (photos != null && photos.Count > 0)
            {
                var sort = 1;
                foreach (var photo in photos)
                {
                    var uploadResult = await _fileService.UploadImageAsync(photo, userId);
                    if (!uploadResult.Success)
                        return ApiResponse<DataKecelakaanDto>.ErrorResponse(
                            uploadResult.ErrorCode!, uploadResult.Message!);

                    evidences.Add(new EvidenceKecelakaan
                    {
                        DataKecelakaanId = dataKecelakaan.Id,
                        FileName = uploadResult.Data!.FileName,
                        GeneratedName = uploadResult.Data.GeneratedName,
                        FilePath = BuildFilePath(uploadResult.Data.GeneratedName),
                        FileSize = uploadResult.Data.FileSize,
                        ContentType = uploadResult.Data.ContentType,
                        SortOrder = sort++,
                        IsActive = true,
                        CreatedBy = userId
                    });
                }
            }

            _context.DataKecelakaans.Add(dataKecelakaan);
            if (evidences.Count > 0)
                _context.EvidenceKecelakaans.AddRange(evidences);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(d => d.Id == dataKecelakaan.Id);
            return ApiResponse<DataKecelakaanDto>.SuccessResponse(
                MapToDto(created, evidences), "Data kecelakaan berhasil ditambahkan");
        }

        public async Task<ApiResponse<DataKecelakaanDto>> UpdateAsync(
            string id, UpdateDataKecelakaanRequest request, List<IFormFile>? photos, string userId)
        {
            var dataKecelakaan = await _context.DataKecelakaans
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (dataKecelakaan == null)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-001", "Data kecelakaan not found");

            // Setiap edit => revisi naik
            dataKecelakaan.Revisi = dataKecelakaan.Revisi + 1;

            var kategoriOk = await _context.KategoriKecelakaans
                .AnyAsync(k => k.Id == request.KategoriId && !k.IsDeleted);
            if (!kategoriOk)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-002", "Kategori tidak ditemukan");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);
            if (!periodeOk)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-010", "Periode tidak ditemukan");

            if (!string.IsNullOrEmpty(request.DriverId))
            {
                var driverOk = await _context.Drivers.AnyAsync(d => d.Id == request.DriverId && !d.IsDeleted);
                if (!driverOk)
                    return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-003", "Driver tidak ditemukan");
            }

            if (!string.IsNullOrEmpty(request.PejabatId))
            {
                var pejabatOk = await _context.Pekerjas.AnyAsync(p => p.Id == request.PejabatId && !p.IsDeleted);
                if (!pejabatOk)
                    return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-004", "Pejabat tidak ditemukan");
            }

            var nomor = request.Nomor.Trim();
            var nomorExists = await _context.DataKecelakaans
                .AnyAsync(d => d.Nomor == nomor && d.Id != id && !d.IsDeleted);
            if (nomorExists)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse("ERR-DATAKECELAKAAN-009", "Nomor laporan sudah ada");

            var tanggal = DateTimeUtil.ParseDateOnly(request.TanggalKejadian);
            if (tanggal == null)
                return ApiResponse<DataKecelakaanDto>.ErrorResponse(
                    "ERR-DATAKECELAKAAN-005", "Tanggal kejadian tidak valid. Format: yyyy-MM-dd.");

            dataKecelakaan.Nomor = nomor;
            dataKecelakaan.Judul = request.Judul.Trim();
            dataKecelakaan.KategoriId = request.KategoriId;
            dataKecelakaan.PeriodeId = request.PeriodeId;
            dataKecelakaan.TanggalKejadian = (DateTime)tanggal;
            dataKecelakaan.WaktuKejadian = request.WaktuKejadian.Trim();
            dataKecelakaan.Dampak = request.Dampak.Trim();
            dataKecelakaan.DriverId = string.IsNullOrWhiteSpace(request.DriverId) ? null : request.DriverId;
            dataKecelakaan.PejabatId = string.IsNullOrWhiteSpace(request.PejabatId) ? null : request.PejabatId;
            dataKecelakaan.Alamat = request.Alamat.Trim();
            dataKecelakaan.DetailKejadian = request.DetailKejadian.Trim();
            dataKecelakaan.PenyebabKejadian = request.PenyebabKejadian.Trim();
            dataKecelakaan.BagaimanaTerjadinya = request.BagaimanaTerjadinya.Trim();
            dataKecelakaan.AkarPermasalahan = request.AkarPermasalahan.Trim();
            dataKecelakaan.TindakanSegara = request.TindakanSegara.Trim();
            dataKecelakaan.TindakanPerbaikan = request.TindakanPerbaikan.Trim();
            dataKecelakaan.ModifiedAt = DateTime.UtcNow;
            dataKecelakaan.ModifiedBy = userId;

            // Foto baru di-append (SortOrder mulai dari max+1)
            var newEvidences = new List<EvidenceKecelakaan>();
            if (photos != null && photos.Count > 0)
            {
                var maxSort = 0;
                var hasEvidence = await _context.EvidenceKecelakaans
                    .AnyAsync(e => !e.IsDeleted && e.DataKecelakaanId == id);
                if (hasEvidence)
                {
                    maxSort = await _context.EvidenceKecelakaans
                        .Where(e => !e.IsDeleted && e.DataKecelakaanId == id)
                        .MaxAsync(e => e.SortOrder);
                }

                var sort = maxSort + 1;
                foreach (var photo in photos)
                {
                    var uploadResult = await _fileService.UploadImageAsync(photo, userId);
                    if (!uploadResult.Success)
                        return ApiResponse<DataKecelakaanDto>.ErrorResponse(
                            uploadResult.ErrorCode!, uploadResult.Message!);

                    newEvidences.Add(new EvidenceKecelakaan
                    {
                        DataKecelakaanId = id,
                        FileName = uploadResult.Data!.FileName,
                        GeneratedName = uploadResult.Data.GeneratedName,
                        FilePath = BuildFilePath(uploadResult.Data.GeneratedName),
                        FileSize = uploadResult.Data.FileSize,
                        ContentType = uploadResult.Data.ContentType,
                        SortOrder = sort++,
                        IsActive = true,
                        CreatedBy = userId
                    });
                }
            }

            if (newEvidences.Count > 0)
                _context.EvidenceKecelakaans.AddRange(newEvidences);
            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(d => d.Id == id);

            var allEvidences = await _context.EvidenceKecelakaans
                .Where(e => !e.IsDeleted && e.DataKecelakaanId == id)
                .OrderBy(e => e.SortOrder)
                .ToListAsync();

            return ApiResponse<DataKecelakaanDto>.SuccessResponse(
                MapToDto(updated, allEvidences), "Data kecelakaan berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var dataKecelakaan = await _context.DataKecelakaans
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (dataKecelakaan == null)
                return ApiResponse<bool>.ErrorResponse("ERR-DATAKECELAKAAN-001", "Data kecelakaan not found");

            var now = DateTime.UtcNow;

            dataKecelakaan.IsDeleted = true;
            dataKecelakaan.DeletedAt = now;
            dataKecelakaan.DeletedBy = userId;

            // Soft-delete semua foto bukti laporan ini juga
            var evidences = await _context.EvidenceKecelakaans
                .Where(e => !e.IsDeleted && e.DataKecelakaanId == id)
                .ToListAsync();

            foreach (var e in evidences)
            {
                e.IsDeleted = true;
                e.DeletedAt = now;
                e.DeletedBy = userId;
            }

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Data kecelakaan deleted");
        }

        /// <summary>Tambah 1 foto bukti ke laporan (gestuurd naar report yang sudah ada).</summary>
        public async Task<ApiResponse<EvidenceKecelakaanDto>> UploadEvidenceAsync(
            string dataKecelakaanId, IFormFile photo, string userId)
        {
            var exists = await _context.DataKecelakaans
                .AnyAsync(d => d.Id == dataKecelakaanId && !d.IsDeleted);

            if (!exists)
                return ApiResponse<EvidenceKecelakaanDto>.ErrorResponse(
                    "ERR-DATAKECELAKAAN-001", "Data kecelakaan not found");

            var uploadResult = await _fileService.UploadImageAsync(photo, userId);
            if (!uploadResult.Success)
                return ApiResponse<EvidenceKecelakaanDto>.ErrorResponse(
                    uploadResult.ErrorCode!, uploadResult.Message!);

            var maxSort = 0;
            var hasEvidence = await _context.EvidenceKecelakaans
                .AnyAsync(e => !e.IsDeleted && e.DataKecelakaanId == dataKecelakaanId);
            if (hasEvidence)
            {
                maxSort = await _context.EvidenceKecelakaans
                    .Where(e => !e.IsDeleted && e.DataKecelakaanId == dataKecelakaanId)
                    .MaxAsync(e => e.SortOrder);
            }

            var evidence = new EvidenceKecelakaan
            {
                DataKecelakaanId = dataKecelakaanId,
                FileName = uploadResult.Data!.FileName,
                GeneratedName = uploadResult.Data.GeneratedName,
                FilePath = BuildFilePath(uploadResult.Data.GeneratedName),
                FileSize = uploadResult.Data.FileSize,
                ContentType = uploadResult.Data.ContentType,
                SortOrder = maxSort + 1,
                IsActive = true,
                CreatedBy = userId
            };

            _context.EvidenceKecelakaans.Add(evidence);
            await _context.SaveChangesAsync();

            return ApiResponse<EvidenceKecelakaanDto>.SuccessResponse(
                MapEvidenceToDto(evidence), "Foto bukti berhasil ditambahkan");
        }

        /// <summary>Soft delete 1 foto bukti (hapus dari form edit).</summary>
        public async Task<ApiResponse<bool>> DeleteEvidenceAsync(string evidenceId, string userId)
        {
            var evidence = await _context.EvidenceKecelakaans
                .FirstOrDefaultAsync(e => e.Id == evidenceId && !e.IsDeleted);

            if (evidence == null)
                return ApiResponse<bool>.ErrorResponse("ERR-EVIDENCEKECELAKAAN-001", "Foto bukti not found");

            evidence.IsDeleted = true;
            evidence.DeletedAt = DateTime.UtcNow;
            evidence.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Foto bukti deleted");
        }

        /// <summary>Stream foto bukti (byte[]) sehingga FE bisa load image via API.</summary>
        public async Task<ApiResponse<EvidenceFileDto>> GetEvidenceImageAsync(string evidenceId)
        {
            var evidence = await _context.EvidenceKecelakaans
                .FirstOrDefaultAsync(e => e.Id == evidenceId && !e.IsDeleted);

            if (evidence == null)
                return ApiResponse<EvidenceFileDto>.ErrorResponse(
                    "ERR-EVIDENCEKECELAKAAN-001", "Foto bukti not found");

            // Evidence.FilePath sudah "{UploadPath}/{ImageFolder}/{GeneratedName}"
            if (!File.Exists(evidence.FilePath))
                return ApiResponse<EvidenceFileDto>.NotFound("Foto bukti tidak ditemukan.");

            var fileBytes = await File.ReadAllBytesAsync(evidence.FilePath);

            return ApiResponse<EvidenceFileDto>.SuccessResponse(new EvidenceFileDto
            {
                FileName = evidence.FileName,
                ContentType = string.IsNullOrEmpty(evidence.ContentType) ? "image/jpeg" : evidence.ContentType,
                Bytes = fileBytes
            });
        }

        /// <summary>Generate PDF formulir penyelidikan kecelakaan (QuestPDF, pattern MCU).</summary>
        public async Task<ApiResponse<FileResult>> GeneratePdfAsync(string id)
        {
            var item = await _context.DataKecelakaans
                .Include(d => d.Kategori)
                .Include(d => d.Periode)
                .Include(d => d.Driver)
                    .ThenInclude(dr => dr!.Vendor)
                .Include(d => d.Pejabat)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (item == null)
                return ApiResponse<FileResult>.ErrorResponse(
                    "ERR-DATAKECELAKAAN-001", "Data kecelakaan not found");

            var evidences = await _context.EvidenceKecelakaans
                .Where(e => !e.IsDeleted && e.DataKecelakaanId == id)
                .OrderBy(e => e.SortOrder)
                .ToListAsync();

            var fotoBytes = new List<byte[]>();
            foreach (var ev in evidences)
            {
                if (File.Exists(ev.FilePath))
                {
                    try
                    {
                        fotoBytes.Add(await File.ReadAllBytesAsync(ev.FilePath));
                    }
                    catch (Exception ex)
                    {
                        // foto corrupt/hilang -> skip, loop lanjut
                    }
                }
            }

            // Logo PPI dari Assets (folder API - dipublish bersama output)
            var logoBytes = new byte[0];
            try
            {
                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo ppi black.png");
                if (File.Exists(logoPath))
                    logoBytes = await File.ReadAllBytesAsync(logoPath);
            }
            catch (Exception ex)
            {
                // logo hilang -> PDF tetap jalan tanpa gambar
            }

            var data = new DataKecelakaanPdfDto
            {
                Nomor = item.Nomor,
                Revisi = FormatRevisi(item.Revisi),
                Status = item.Status,
                LogoBytes = logoBytes,
                Judul  = "AWAL KEJADIAN KECELAKAAN KERJA",   // banner text (red bar)
                Judul2 = item.Judul,
                Tanggal = $"{item.TanggalKejadian:dd-MM-yyyy}",
                Waktu = item.WaktuKejadian,
                Dampak = item.Dampak,
                KategoriName = item.Kategori?.Name ?? string.Empty,
                PeriodeName = item.Periode?.NamaPeriode ?? string.Empty,
                Alamat = item.Alamat,
                DriverInfo = item.Driver != null
                    ? item.Driver.NoPekerja + " - " + item.Driver.NamaDriver
                    : "-",
                PejabatInfo = item.Pejabat != null
                    ? item.Pejabat.NoPekerja + " - " + item.Pejabat.NamaPekerja
                    : string.Empty,
                DetailKejadian = item.DetailKejadian,
                PenyebabKejadian = item.PenyebabKejadian,
                BagaimanaTerjadinya = item.BagaimanaTerjadinya,
                AkarPermasalahan = StripHtml(item.AkarPermasalahan),
                TindakanSegara = StripHtml(item.TindakanSegara),
                TindakanPerbaikan = StripHtml(item.TindakanPerbaikan),
                FotoBukti = fotoBytes
            };

            var document = new DataKecelakaanPdfDocument(data);
            var pdfBytes = document.GeneratePdf();

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = $"Kecelakaan_{item.Nomor}.pdf",
                ContentType = "application/pdf",
                FileStream = new MemoryStream(pdfBytes)
            });
        }

        /// <summary>
        /// Strip HTML tags dari konten rich-text editor (di-export ke PDF sebagai teks).
        /// Manual iterasi - regex TIDAK tersedia di dialect ini.
        /// </summary>
        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "-";

            var chars = new List<char>();
            var inTag = false;

            foreach (var c in html)
            {
                if (c == '<')
                {
                    inTag = true;
                    continue;
                }
                if (c == '>')
                {
                    inTag = false;
                    continue;
                }
                if (!inTag)
                {
                    if (c == '\n' || c == '\r')
                        chars.Add(' ');
                    else
                        chars.Add(c);
                }
            }

            var result = new string(chars.ToArray()).Trim();
            return result.Length == 0 ? "-" : result;
        }

        /// <summary>Revisi sebagai "00", "01", ... (display di header PDF).</summary>
        private static string FormatRevisi(int revisi)
        {
            if (revisi < 10)
                return $"0{revisi}";
            return $"{revisi}";
        }

        // =========================================================
        // SUMMARY: total laporan + split status + total foto bukti
        // =========================================================
        public async Task<ApiResponse<DataKecelakaanSummaryDto>> GetSummaryAsync(DataKecelakaanSummaryRequest filter)
        {
            string? namaPeriode = null;

            if (!string.IsNullOrEmpty(filter.PeriodeId))
            {
                namaPeriode = await _context.Periodes
                    .Where(p => p.Id == filter.PeriodeId && !p.IsDeleted)
                    .Select(p => p.NamaPeriode)
                    .FirstOrDefaultAsync();
            }

            var query = _context.DataKecelakaans.Where(d => !d.IsDeleted);

            if (!string.IsNullOrEmpty(filter.KategoriId))
                query = query.Where(d => d.KategoriId == filter.KategoriId);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(d => d.PeriodeId == filter.PeriodeId);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(d => d.Status == filter.Status);

            var ids = await query.Select(d => d.Id).ToListAsync();

            var totalDataKecelakaan = ids.Count;
            var totalPublished = await _context.DataKecelakaans.CountAsync(d =>
                !d.IsDeleted && ids.Contains(d.Id) && d.Status == DataKecelakaan.StatusPublished);
            var totalDraft = await _context.DataKecelakaans.CountAsync(d =>
                !d.IsDeleted && ids.Contains(d.Id) && d.Status == DataKecelakaan.StatusDraft);

            var totalFotoBukti = await _context.EvidenceKecelakaans.CountAsync(e =>
                !e.IsDeleted && ids.Contains(e.DataKecelakaanId));

            return ApiResponse<DataKecelakaanSummaryDto>.SuccessResponse(new DataKecelakaanSummaryDto
            {
                PeriodeId = filter.PeriodeId,
                NamaPeriode = namaPeriode,
                TotalDataKecelakaan = totalDataKecelakaan,
                TotalPublished = totalPublished,
                TotalDraft = totalDraft,
                TotalFotoBukti = totalFotoBukti
            });
        }

        private string BuildFilePath(string generatedName) =>
            Path.Combine(_storage.UploadPath ?? string.Empty, _storage.ImageFolder ?? string.Empty, generatedName);

        private string BuildEvidenceUrl(string generatedName) =>
            $"{_storage.BaseUrl}{_storage.ImageFolder}/{generatedName}";

        private EvidenceKecelakaanDto MapEvidenceToDto(EvidenceKecelakaan e) => new()
        {
            Id = e.Id,
            DataKecelakaanId = e.DataKecelakaanId,
            FileName = e.FileName,
            GeneratedName = e.GeneratedName,
            FileSize = e.FileSize,
            ContentType = e.ContentType,
            SortOrder = e.SortOrder,
            Url = BuildEvidenceUrl(e.GeneratedName)
        };

        private DataKecelakaanDto MapToDto(DataKecelakaan d, List<EvidenceKecelakaan> evidences) => new()
        {
            Id = d.Id,
            Nomor = d.Nomor,
            Judul = d.Judul,
            KategoriId = d.KategoriId,
            KategoriName = d.Kategori?.Name ?? string.Empty,
            PeriodeId = d.PeriodeId,
            NamaPeriode = d.Periode?.NamaPeriode ?? string.Empty,
            TanggalKejadian = d.TanggalKejadian,
            WaktuKejadian = d.WaktuKejadian,
            Dampak = d.Dampak,
            DriverId = d.DriverId,
            DriverInfo = d.Driver != null ? d.Driver.NoPekerja + " - " + d.Driver.NamaDriver : string.Empty,
            PejabatId = d.PejabatId,
            PejabatInfo = d.Pejabat != null ? d.Pejabat.NoPekerja + " - " + d.Pejabat.NamaPekerja : string.Empty,
            Alamat = d.Alamat,
            DetailKejadian = d.DetailKejadian,
            PenyebabKejadian = d.PenyebabKejadian,
            BagaimanaTerjadinya = d.BagaimanaTerjadinya,
            AkarPermasalahan = d.AkarPermasalahan,
            TindakanSegara = d.TindakanSegara,
            TindakanPerbaikan = d.TindakanPerbaikan,
            Status = d.Status,
            Revisi = d.Revisi,
            Evidences = evidences != null ? evidences.Select(MapEvidenceToDto).ToList() : new List<EvidenceKecelakaanDto>(),
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt,
            ModifiedAt = d.ModifiedAt
        };
    }
}