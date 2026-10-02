using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ApiService.Domain.Entities;
using ApiService.Application.Interfaces;
using ApiService.Application.DTOs;

namespace ApiService.Infrastructure.Persistence
{
    public class ServiceDbContext : DbContext, IServiceDbContext
    {
        // ===================================
        // ADD YOUR DBSETS HERE
        // ===================================
        public DbSet<Product> Products { get; set; }
        public DbSet<FeedbackEntity> Feedbacks { get; set; }
        // view ke table user
        public DbSet<GetDataUsers> Users { get; set; }
        public DbSet<GetDataUserRolesApprover> UserRolesApprover { get; set; }
        public DbSet<MasterJabatan> Jabatans { get; set; }
        public DbSet<Pekerja> Pekerjas { get; set; }
        public DbSet<MasterBahanBakar> BahanBakars { get; set; }
        public DbSet<MasterKepemilikan> Kepemilikans { get; set; }
        public DbSet<MasterTipe> Tipes { get; set; }
        public DbSet<MasterJenisBbm> JenisBbms { get; set; }
        public DbSet<MasterKategoriKecelakaan> KategoriKecelakaans { get; set; }
        public DbSet<DataKecelakaan> DataKecelakaans { get; set; }
        public DbSet<EvidenceKecelakaan> EvidenceKecelakaans { get; set; }
        public DbSet<MasterVendor> Vendors { get; set; }
        public DbSet<Kendaraan> Kendaraans { get; set; }
        public DbSet<RfId> RfIds { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Periode> Periodes { get; set; }
        public DbSet<MemberParkir> MemberParkirs { get; set; }
        public DbSet<OperasionalUpah> OperasionalUpahs { get; set; }
        public DbSet<BbmSubmission> BbmSubmissions { get; set; }
        public DbSet<TagihanKwh> TagihanKwhs { get; set; }
        public DbSet<PerjalananDinas> PerjalananDinas { get; set; }
        public DbSet<SimCard> SimCards { get; set; }
        public DbSet<BiayaKesehatan> BiayaKesehatans { get; set; }
        public DbSet<OperasionalTagihanBbmRekonsiliasi> OperasionalTagihanBbmRekonsiliasis { get; set; }
        public ServiceDbContext(DbContextOptions<ServiceDbContext> options) : base(options) { }

        // mobile
        public DbSet<DailyCheckUpEntity> DailyCheckUpEntities { get; set; }
        public DbSet<DailyCheckUpEvidance> DailyCheckUpEvidances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===================================
            // EXAMPLE: Product configuration
            // Copy this block for each new entity
            // ===================================
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // ------------------------------------------------------------
            // USER
            // ------------------------------------------------------------
            modelBuilder.Entity<GetDataUsers>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_Users");
            });

            modelBuilder.Entity<GetDataUserRolesApprover>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_user_roles");
            });

            modelBuilder.Entity<MasterJabatan>(entity =>
            {
                entity.ToTable("MasterJabatans");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<MasterBahanBakar>(entity =>
            {
                entity.ToTable("MasterBahanBakars");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<MasterKepemilikan>(entity =>
            {
                entity.ToTable("MasterKepemilikans");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<MasterTipe>(entity =>
            {
                entity.ToTable("MasterTipes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<MasterJenisBbm>(entity =>
            {
                // Nama tabel sesuai request: MasterJenisBbm (aja)
                entity.ToTable("MasterJenisBbm");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<MasterKategoriKecelakaan>(entity =>
            {
                entity.ToTable("MasterKategoriKecelakaan");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<MasterVendor>(entity =>
            {
                entity.ToTable("MasterVendors");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // ------------------------------------------------------------
            // PEKERJA (Data Master > Pekerja)
            // ------------------------------------------------------------
            var rfIdsComparer = new ValueComparer<List<string>>(
                (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                v => v == null ? 0 : v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s)),
                v => v == null ? new List<string>() : v.ToList());

            modelBuilder.Entity<Pekerja>(entity =>
            {
                entity.ToTable("Pekerjas");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.NoPekerja).IsUnique();
                entity.HasIndex(e => e.NamaPekerja);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Jabatan)
                    .WithMany()
                    .HasForeignKey(e => e.JabatanId)
                    .OnDelete(DeleteBehavior.Restrict);

                // RfIds (List<string>) disimpan sebagai JSON di 1 kolom nvarchar(max)
                entity.Property(e => e.RfIds)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v ?? new List<string>(), (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Metadata.SetValueComparer(rfIdsComparer);
            });

            // ------------------------------------------------------------
            // KENDARAAN (Data Master > Kendaraan)
            // ------------------------------------------------------------
            modelBuilder.Entity<Kendaraan>(entity =>
            {
                entity.ToTable("Kendaraans");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.NomorPolisi).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Tipe)
                    .WithMany()
                    .HasForeignKey(e => e.TipeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.BahanBakar)
                    .WithMany()
                    .HasForeignKey(e => e.BahanBakarId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Kepemilikan)
                    .WithMany()
                    .HasForeignKey(e => e.KepemilikanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Jabatan)
                    .WithMany()
                    .HasForeignKey(e => e.JabatanId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Pejabat pemegang kendaraan - opsional (nullable FK)
                entity.HasOne(e => e.Pekerja)
                    .WithMany()
                    .HasForeignKey(e => e.PekerjaId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            // ------------------------------------------------------------
            // RF.ID (Data Master > RF.ID)
            // ------------------------------------------------------------
            modelBuilder.Entity<RfId>(entity =>
            {
                entity.ToTable("RfIds");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RfIdCode).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Pekerja)
                    .WithMany()
                    .HasForeignKey(e => e.PekerjaId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne(e => e.Kendaraan)
                    .WithMany()
                    .HasForeignKey(e => e.KendaraanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // DRIVER (Data Master > Driver)
            // ------------------------------------------------------------
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.ToTable("Drivers");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.NoPekerja).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Vendor)
                    .WithMany()
                    .HasForeignKey(e => e.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Atasan - opsional (nullable FK), boleh kosong saat masa transisi jabatan
                entity.HasOne(e => e.Atasan)
                    .WithMany()
                    .HasForeignKey(e => e.AtasanId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });

            // ------------------------------------------------------------
            // PERIODE (Data Master > Periode) - master independen
            // ------------------------------------------------------------
            modelBuilder.Entity<Periode>(entity =>
            {
                entity.ToTable("Periodes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.NamaPeriode).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(false);
            });

            // ------------------------------------------------------------
            // MEMBER PARKIR (Data Master > Member Parkir)
            // Pekerja + Jabatan (snapshot otomatis dari Pekerja). RF.ID TIDAK
            // disimpan di sini - selalu ditarik dari Pekerja.RfIds saat ditampilkan.
            // ------------------------------------------------------------
            modelBuilder.Entity<MemberParkir>(entity =>
            {
                entity.ToTable("MemberParkir");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PekerjaId);
                entity.HasIndex(e => e.JabatanId);
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.JumlahBiaya).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Pekerja)
                    .WithMany()
                    .HasForeignKey(e => e.PekerjaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Jabatan)
                    .WithMany()
                    .HasForeignKey(e => e.JabatanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // OPERASIONAL & UPAH (Data Master > Operasional & Upah)
            // Diinput admin, 1 baris per Pekerja per Periode. RF.ID/No.Pekerja/
            // Jabatan TIDAK disimpan - selalu ditarik dari Pekerja. Total BBM juga
            // TIDAK di sini - lihat BbmSubmission (butuh approval terpisah).
            // ------------------------------------------------------------
            modelBuilder.Entity<OperasionalUpah>(entity =>
            {
                entity.ToTable("OperasionalUpah");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PekerjaId, e.PeriodeId }).IsUnique();
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.TotalLembur).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalEMoneyMember).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DanaOps).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalParkir).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalSewaKendaraan).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalUpahDriver).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Pekerja)
                    .WithMany()
                    .HasForeignKey(e => e.PekerjaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // BBM SUBMISSION (Persetujuan Tertunda > Pengajuan BBM Driver)
            // Diajukan driver dari mobile (nota), bisa banyak baris per Pekerja per
            // Periode, wajib approval sebelum ikut dihitung ke dashboard.
            // ------------------------------------------------------------
            modelBuilder.Entity<BbmSubmission>(entity =>
            {
                entity.ToTable("BbmSubmissions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.DriverId);
                entity.HasIndex(e => e.AtasanPekerjaId);
                entity.HasIndex(e => e.KendaraanId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.JumlahPenggunaanBbm).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NilaiOdometer).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NilaiNota).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(20);
                entity.Property(e => e.FotoOdometerUrl).HasMaxLength(500);
                entity.Property(e => e.FotoNotaUrl).HasMaxLength(500);
                entity.Property(e => e.CatatanTambahan).HasMaxLength(500);
                entity.Property(e => e.RejectedReason).HasMaxLength(500);

                entity.HasOne(e => e.Driver)
                    .WithMany()
                    .HasForeignKey(e => e.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Snapshot Atasan/VP - opsional (nullable FK), boleh kosong kalau
                // Driver sedang tidak punya Atasan pada saat submit
                entity.HasOne(e => e.AtasanPekerja)
                    .WithMany()
                    .HasForeignKey(e => e.AtasanPekerjaId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Kendaraan)
                    .WithMany()
                    .HasForeignKey(e => e.KendaraanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // TAGIHAN KWH (Data Master > Tagihan)
            // ------------------------------------------------------------
            modelBuilder.Entity<TagihanKwh>(entity =>
            {
                entity.ToTable("TagihanKwhs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.JumlahBiaya).HasColumnType("decimal(18,2)");
                entity.Property(e => e.JumlahKwh).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Kategori).HasMaxLength(20).HasDefaultValue(TagihanKwh.KategoriP8);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // PERJALANAN DINAS (Data Master > Perjalanan Dinas)
            // ------------------------------------------------------------
            modelBuilder.Entity<PerjalananDinas>(entity =>
            {
                entity.ToTable("PerjalananDinas");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PekerjaId);
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.BulanTahun);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.TotalBiayaDinas).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Pekerja)
                    .WithMany()
                    .HasForeignKey(e => e.PekerjaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SimCard>(entity =>
            {
                entity.ToTable("SimCards");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PekerjaId);
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.BiayaSimCard).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Pekerja)
                    .WithMany()
                    .HasForeignKey(e => e.PekerjaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // BIAYA KESEHATAN - khusus periode + bulan/tahun
            // ------------------------------------------------------------
            modelBuilder.Entity<BiayaKesehatan>(entity =>
            {
                entity.ToTable("BiayaKesehatans");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PekerjaId);
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.BulanTahun);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.TotalBiaya).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Pekerja)
                    .WithMany()
                    .HasForeignKey(e => e.PekerjaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // BIAYA KESEHATAN
            // ------------------------------------------------------------
            modelBuilder.Entity<BiayaKesehatan>(entity =>
            {
                entity.ToTable("BiayaKesehatans");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PekerjaId);
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.BulanTahun);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.TotalBiaya).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Pekerja)
                    .WithMany()
                    .HasForeignKey(e => e.PekerjaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // DATA KECELAKAAN (laporan kecelakaan utama)
            // ------------------------------------------------------------
            modelBuilder.Entity<DataKecelakaan>(entity =>
            {
                entity.ToTable("DataKecelakaans");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.KategoriId);
                entity.HasIndex(e => e.PeriodeId);
                entity.HasIndex(e => e.DriverId);
                entity.HasIndex(e => e.PejabatId);
                entity.HasIndex(e => e.TanggalKejadian);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Nomor).IsUnique();
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Status).HasDefaultValue(DataKecelakaan.StatusDraft);
                entity.Property(e => e.Revisi).HasDefaultValue(0);

                // Rich text => nvarchar(max)
                entity.Property(e => e.AkarPermasalahan).HasColumnType("nvarchar(max)");
                entity.Property(e => e.TindakanSegara).HasColumnType("nvarchar(max)");
                entity.Property(e => e.TindakanPerbaikan).HasColumnType("nvarchar(max)");

                // WaktuKejadian: string "HH:mm"
                entity.Property(e => e.WaktuKejadian).HasMaxLength(5);

                entity.HasOne(e => e.Kategori)
                    .WithMany()
                    .HasForeignKey(e => e.KategoriId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Periode)
                    .WithMany()
                    .HasForeignKey(e => e.PeriodeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Driver)
                    .WithMany()
                    .HasForeignKey(e => e.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Pejabat)
                    .WithMany()
                    .HasForeignKey(e => e.PejabatId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ------------------------------------------------------------
            // EVIDENCE KECELAKAAN (foto bukti, 1:N ke DataKecelakaan)
            // ------------------------------------------------------------
            modelBuilder.Entity<EvidenceKecelakaan>(entity =>
            {
                entity.ToTable("EvidenceKecelakaans");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DataKecelakaanId);
                entity.HasIndex(e => e.GeneratedName).IsUnique();
                entity.HasIndex(e => e.SortOrder);
                entity.HasIndex(e => e.IsDeleted);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.FileSize).HasColumnType("bigint");

                entity.HasOne(e => e.DataKecelakaan)
                    .WithMany()
                    .HasForeignKey(e => e.DataKecelakaanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DailyCheckUpEntity>(entity =>
            {
                entity.ToTable("DailyCheckUps");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.DriverId);
                entity.HasIndex(e => e.TanggalDcu);
                entity.HasIndex(e => e.StatusKesehatan);
                entity.HasIndex(e => e.IsDeleted);

                entity.HasIndex(e => new { e.DriverId, e.TanggalDcu })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.StatusKesehatan)
                    .HasDefaultValue(DailyCheckUpEntity.StatusFit);

                entity.Property(e => e.TekananDarahSistolik)
                    .HasPrecision(18, 2);

                entity.Property(e => e.TekananDarahDiastolik)
                    .HasPrecision(18, 2);

                entity.Property(e => e.SaturasiOksigen)
                    .HasPrecision(18, 2);

                entity.Property(e => e.NadiDenyut)
                    .HasPrecision(18, 2);

                entity.Property(e => e.SuhuTubuh)
                    .HasPrecision(18, 2);

                entity.Property(e => e.Keterangan)
                    .HasMaxLength(1000);

                entity.HasOne(e => e.Driver)
                    .WithMany()
                    .HasForeignKey(e => e.DriverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DailyCheckUpEvidance>(entity =>
            {
                entity.ToTable("DailyCheckUpEvidences");
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.DataDcuId);
                entity.HasIndex(e => e.GeneratedName)
                      .IsUnique();

                entity.HasIndex(e => e.SortOrder);
                entity.HasIndex(e => e.IsDeleted);

                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                entity.Property(e => e.FileSize)
                      .HasColumnType("bigint");

                entity.HasOne(e => e.DataDcu)
                      .WithMany(e => e.Evidences)
                      .HasForeignKey(e => e.DataDcuId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}