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
        public DbSet<MasterVendor> Vendors { get; set; }
        public DbSet<Kendaraan> Kendaraans { get; set; }
        public DbSet<RfId> RfIds { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public ServiceDbContext(DbContextOptions<ServiceDbContext> options) : base(options) { }

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
        }
    }
}
