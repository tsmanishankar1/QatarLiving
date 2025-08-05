using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using QLN.Common.DTO_s.ClassifiedsBo;
using System.Reflection.Metadata;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLClassifiedContext : DbContext
    {
        public QLClassifiedContext(DbContextOptions<QLClassifiedContext> options)
            : base(options)
        {
        }

        
        public DbSet<StoresSubscriptionDto> StoresSubscriptions { get; set; }
        public DbSet<SubscriptionTypes> SubscriptionType {  get; set; }
        public DbSet<StoreStatus> StoreStatuses { get; set; }
        
        public DbSet<StoreProducts> StoreProduct { get; set; }
        public DbSet<ProductFeatures> ProductFeature { get; set; }
        public DbSet<ProductImages> ProductImage { get; set; }
        public DbSet<ProductPageCoordinates> ProductPageCoordinate { get; set; }
        public DbSet<StoreFlyers> StoreFlyer { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoreFlyers>()
            .HasMany(s=>s.Products)
            .WithOne(s=>s.StoreFlyer)
            .HasForeignKey(s=>s.FlyerId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoreProducts>()
        .HasMany(s => s.Features)
        .WithOne(a => a.StoreProduct)
        .HasForeignKey(a => a.StoreProductId)
        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoreProducts>()
          .HasOne(s => s.PageCoordinates)
          .WithOne(a => a.StoreProduct)
          .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoreProducts>()
  .HasMany(s => s.Images)
  .WithOne(a => a.StoreProduct)
  .HasForeignKey(a => a.StoreProductId)
  .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
    
}
