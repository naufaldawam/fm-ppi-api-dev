using System;
using System.Collections.Generic;
using System.IO;

namespace ApiService.Application.DTOs.Mobile
{

    // =========================================================================
    // =================== Daily Check Up / DCU DTOs ===================
    // =========================================================================
    public class CreateDcuRequest
    {
        public string DriverId { get; set; } = string.Empty;
        public DateTime TanggalDcu { get; set; }
        public decimal TekananDarahSistolik { get; set; }
        public decimal TekananDarahDiastolik { get; set; }
        public decimal SaturasiOksigen { get; set; }
        public decimal NadiDenyut { get; set; }
        public decimal SuhuTubuh { get; set; }
        public string StatusKesehatan { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
    }

    public class UpdateDcuRequest
    {
        public string DriverId { get; set; } = string.Empty;
        public DateTime TanggalDcu { get; set; }
        public decimal TekananDarahSistolik { get; set; }
        public decimal TekananDarahDiastolik { get; set; }
        public decimal SaturasiOksigen { get; set; }
        public decimal NadiDenyut { get; set; }
        public decimal SuhuTubuh { get; set; }
        public string StatusKesehatan { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DataDcuDto
    {
        public string Id { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public string NoPekerjaDriver { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public DateTime TanggalDcu { get; set; }
        public decimal TekananDarahSistolik { get; set; }
        public decimal TekananDarahDiastolik { get; set; }
        public decimal SaturasiOksigen { get; set; }
        public decimal NadiDenyut { get; set; }
        public decimal SuhuTubuh { get; set; }
        public string StatusKesehatan { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public bool IsActive { get; set; }
        public EvidenceDcuDto? Evidence { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class EvidenceDcuDto
    {
        public string Id { get; set; } = string.Empty;
        public string DataDcuId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string GeneratedName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class DataDcuFilterRequest
    {
        public string? Search { get; set; }
        public string? DriverId { get; set; }
        public string? StatusKesehatan { get; set; }
        public DateTime? TanggalFrom { get; set; }
        public DateTime? TanggalTo { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // =========================================================================
    // =================== Inspeksi Kendaran DTOs ===================
    // =========================================================================
    public class InspeksiKendaraanFilterRequest
    {
        public string? Search { get; set; }
        public string? KendaraanId { get; set; }
        public string? DriverId { get; set; }
        public DateTime? TanggalInspeksi { get; set; }
        public string? KelayakanJalan { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CreateInspeksiKendaraanRequest
    {
        public string KendaraanId { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public DateTime TanggalInspeksi { get; set; }
        public string? Keterangan { get; set; }
        public List<CreateInspeksiKendaraanDetailRequest> Details { get; set; } = new();
    }

    public class UpdateInspeksiKendaraanRequest
    {
        public string KendaraanId { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public DateTime TanggalInspeksi { get; set; }
        public string? Keterangan { get; set; }
        public List<UpdateInspeksiKendaraanDetailRequest> Details { get; set; } = new();
    }

    public class CreateInspeksiKendaraanDetailRequest
    {
        public string Kategori { get; set; } = string.Empty;
        public string NamaPemeriksaan { get; set; } = string.Empty;
        public bool IsOk { get; set; }
        public string? Keterangan { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpdateInspeksiKendaraanDetailRequest
    {
        public string Kategori { get; set; } = string.Empty;
        public string NamaPemeriksaan { get; set; } = string.Empty;
        public bool IsOk { get; set; }
        public string? Keterangan { get; set; }
        public int SortOrder { get; set; }
    }

    public class InspeksiKendaraanDto
    {
        public string Id { get; set; } = string.Empty;
        public string KendaraanId { get; set; } = string.Empty;
        public string NomorPolisi { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public string NoPekerjaDriver { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public DateTime TanggalInspeksi { get; set; }
        public string KelayakanJalan { get; set; } = string.Empty;
        public bool LayakJalan => string.Equals(
            KelayakanJalan,
            Domain.Entities.Mobile.InspeksiKendaraanEntity.StatusLayakJalan,
            StringComparison.OrdinalIgnoreCase);
        public string? Keterangan { get; set; }
        public bool IsActive { get; set; }
        public List<InspeksiKendaraanDetailDto> Details { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class InspeksiKendaraanDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Kategori { get; set; } = string.Empty;
        public string NamaPemeriksaan { get; set; } = string.Empty;
        public bool IsOk { get; set; }
        public string Status => IsOk ? "OK" : "Tidak OK";
        public string? Keterangan { get; set; }
        public int SortOrder { get; set; }
    }

    // =========================================================================
    // =================== Perjalanan Dinas DTOs ===================
    // =========================================================================

    public class CreatePerjalananDinasMobileRequest
    {
        public string PekerjaId { get; set; } = string.Empty;
        public string BulanTahun { get; set; } = string.Empty;
        public string PeriodeId { get; set; } = string.Empty;
        public decimal TotalBiayaDinas { get; set; }
    }

    public class UpdatePerjalananDinasMobileRequest
    {
        public string PekerjaId { get; set; } = string.Empty;
        public string BulanTahun { get; set; } = string.Empty;
        public string PeriodeId { get; set; } = string.Empty;
        public decimal TotalBiayaDinas { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PerjalananDinasMobileFilterRequest
    {
        public string? Search { get; set; }
        public string? PeriodeId { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}