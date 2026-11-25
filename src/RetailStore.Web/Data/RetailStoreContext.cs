using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RetailStore.Web.Models;

namespace RetailStore.Web.Data;

public partial class RetailStoreContext : DbContext
{
    public RetailStoreContext(DbContextOptions<RetailStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<DemandForecast> DemandForecasts { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleItem> SaleItems { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<VwWeeklySale> VwWeeklySales { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Categories_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<DemandForecast>(entity =>
        {
            entity.HasKey(e => e.ForecastId);

            entity.HasIndex(e => new { e.ProductId, e.WeekStart }, "UQ_DemandForecasts_Product_Week").IsUnique();

            entity.Property(e => e.CalculatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_DemandForecasts_CalculatedAt");
            entity.Property(e => e.ForecastQty).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.DemandForecasts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_DemandForecasts_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Products_CategoryId");

            entity.HasIndex(e => e.SupplierId, "IX_Products_SupplierId");

            entity.HasIndex(e => e.Name, "UQ_Products_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Products)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Suppliers");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasIndex(e => e.CustomerId, "IX_Sales_CustomerId");

            entity.HasIndex(e => e.SaleDate, "IX_Sales_SaleDate");

            entity.Property(e => e.SaleDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())", "DF_Sales_SaleDate");

            entity.HasOne(d => d.Customer).WithMany(p => p.Sales)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Sales_Customers");
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.ToTable(tb => tb.HasTrigger("trg_SaleItems_UpdateStock"));

            entity.HasIndex(e => e.ProductId, "IX_SaleItems_ProductId");

            entity.HasIndex(e => new { e.SaleId, e.ProductId }, "UQ_SaleItems_Sale_Product").IsUnique();

            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SaleItems_Products");

            entity.HasOne(d => d.Sale).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("FK_SaleItems_Sales");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(e => e.Name, "UQ_Suppliers_Name").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<VwWeeklySale>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_WeeklySales");

            entity.Property(e => e.Revenue).HasColumnType("decimal(38, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
