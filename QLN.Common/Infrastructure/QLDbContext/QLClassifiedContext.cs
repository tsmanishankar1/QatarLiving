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

        //public DbSet<StoresDto> storesDtos { get; set; }
        public DbSet<StoresSubscriptionDto> StoresSubscriptions { get; set; }
        public DbSet<SubscriptionTypes> SubscriptionType {  get; set; }
        public DbSet<StoreStatus> StoreStatuses { get; set; }
        //public DbSet<Stores> Store { get; set; }
        //public DbSet<StoreAddresses> StoreAddress { get; set; }
        //public DbSet<StoreLicenseDocuments> StoreLicense { get; set; }
        //public DbSet<StoreProductDocuments> StoreDocuments { get; set; }

        public DbSet<StoreProducts> StoreProduct { get; set; }
        public DbSet<ProductFeatures> ProductFeature { get; set; }
        public DbSet<ProductImages> ProductImage { get; set; }
        
        public DbSet<Items> Item { get; set; }

        public DbSet<Preloveds> Preloved { get; set; }
        public DbSet<Collectibles> Collectible { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Items>()
                .Property(p => p.Attributes)
                .HasColumnType("jsonb");

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


         //   modelBuilder.Entity<Stores>()
         //.HasMany(s => s.Addresses)
         //.WithOne(a => a.Store)
         //.HasForeignKey(a => a.StoresID)
         //.OnDelete(DeleteBehavior.Cascade);

         //   modelBuilder.Entity<Stores>()
         //       .HasMany(s => s.LicenseDocuments)
         //       .WithOne(ld => ld.Store)
         //       .HasForeignKey(ld => ld.StoresID)
         //       .OnDelete(DeleteBehavior.Cascade);

         //   modelBuilder.Entity<Stores>()
         //       .HasMany(s => s.ProductDocuments)
         //       .WithOne(pd => pd.Store)
         //       .HasForeignKey(pd => pd.StoresID)
         //       .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
    
}
