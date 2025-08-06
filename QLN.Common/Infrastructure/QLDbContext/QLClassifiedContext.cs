using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Model;
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
        public DbSet<ServicesCategory> ServicesCategories { get; set; }
        public DbSet<L1Category> L1Categories { get; set; }
        public DbSet<L2Category> L2Categories { get; set; }
        public DbSet<Services> Services { get; set; }
        public DbSet<Items> Item { get; set; }
        public DbSet<StoreFlyers> StoreFlyer { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoreFlyers>()
                .HasMany(s => s.Products)
                .WithOne(s => s.StoreFlyer)
                .HasForeignKey(s => s.FlyerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoreProducts>()
                .HasMany(s => s.Features)
                .WithOne(a => a.StoreProduct)
                .HasForeignKey(a => a.StoreProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoreProducts>()
                .HasMany(s => s.Images)
                .WithOne(a => a.StoreProduct)
                .HasForeignKey(a => a.StoreProductId)
                .OnDelete(DeleteBehavior.Cascade);
           
            modelBuilder.Entity<ServicesCategory>()
                .HasMany(sc => sc.L1Categories)
                .WithOne(l1 => l1.ServicesCategory)
                .HasForeignKey(l1 => l1.ServicesCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<L1Category>()
                .HasMany(l1 => l1.L2Categories)
                .WithOne(l2 => l2.L1Category)
                .HasForeignKey(l2 => l2.L1CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ServicesCategory>()
                .HasMany(sc => sc.Services)
                .WithOne(s => s.Category)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.NoAction); 
            modelBuilder.Entity<L1Category>()
                .HasMany(l1 => l1.Services)
                .WithOne(s => s.L1Category)
                .HasForeignKey(s => s.L1CategoryId)
                .OnDelete(DeleteBehavior.NoAction); 
            modelBuilder.Entity<L2Category>()
                .HasMany(l2 => l2.Services)
                .WithOne(s => s.L2Category)
                .HasForeignKey(s => s.L2CategoryId)
                .OnDelete(DeleteBehavior.NoAction); 
            modelBuilder.Entity<Services>()
                .Property(s => s.PhotoUpload)
                .HasColumnType("jsonb");

            base.OnModelCreating(modelBuilder);
        }

    }

}
