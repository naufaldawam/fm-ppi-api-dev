using System;
using Microsoft.EntityFrameworkCore;
using MyService.Domain.Entities;
using MyService.Application.Interfaces;

namespace MyService.Infrastructure.Persistence
{
    public class ServiceDbContext : DbContext, IServiceDbContext
    {
        // ===================================
        // ADD YOUR DBSETS HERE
        // ===================================
        public DbSet<Product> Products { get; set; }

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
        }
    }
}
