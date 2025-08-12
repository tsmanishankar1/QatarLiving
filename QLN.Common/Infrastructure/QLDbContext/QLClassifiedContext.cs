using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsFo;
using QLN.Common.Infrastructure.Model;


namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLClassifiedContext : DbContext
    {
        public QLClassifiedContext(DbContextOptions<QLClassifiedContext> options)
            : base(options)
        {
        }
        public DbSet<StoresSubscriptionDto> StoresSubscriptions { get; set; }
        //public DbSet<SubscriptionTypes> SubscriptionType {  get; set; }
        public DbSet<StoreStatus> StoreStatuses { get; set; }
        public DbSet<StoreProducts> StoreProduct { get; set; }
        public DbSet<ProductFeatures> ProductFeature { get; set; }
        public DbSet<ProductImages> ProductImage { get; set; }
        public DbSet<Items> Item { get; set; }
        public DbSet<Deals> Deal { get; set; }
        public DbSet<Preloveds> Preloved { get; set; }
        public DbSet<Collectibles> Collectible { get; set; }
        public DbSet<StoreFlyers> StoreFlyer { get; set; }
        public DbSet<Services> Services { get; set; }
        public DbSet<Category> Categories { get; set; }
        
        public DbSet<SeasonalPicks> SeasonalPicks { get; set; }

        public DbSet<FeaturedStore> FeaturedStores { get; set; }
        public DbSet<FeaturedCategory> FeaturedCategories { get; set; }

        public DbSet<StoresDashboardHeader> StoresDashboardHeaderItems { get; set; }
        public DbSet<StoresDashboardSummary> StoresDashboardSummaryItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StoreFlyers>()
                .HasMany(s => s.Products)
                .WithOne(s => s.StoreFlyer)
                .HasForeignKey(s => s.FlyerId)
                .OnDelete(DeleteBehavior.Cascade);

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
            modelBuilder.Entity<Category>()

                 .HasMany(sc => sc.CategoryFields)
                 .WithOne(sc => sc.ParentCategory)
                 .HasForeignKey(sc => sc.ParentId)
                 .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<StoresDashboardHeader>(entity =>
            {
                entity.HasNoKey(); 
                entity.ToSqlQuery(@"
            SELECT subs.""CompanyId"",
                subs.""SubscriptionId"",
                subs.""ProductName"",
                subs.""UserId"",
                comp.""UserName"",
                comp.""Status"",
                comp.""CompanyName"",
                comp.""CompanyLogo"",
                subs.""StartDate"",
                subs.""EndDate"",
                '' as ""XMLFeed"",
            '' as ""UploadFeed""
            FROM public.""Subscriptions"" AS subs
            INNER JOIN public.""Companies"" AS comp
                ON subs.""CompanyId"" = comp.""Id""
            WHERE subs.""Status"" = 1 
              AND subs.""Vertical"" = 3
              AND subs.""SubVertical"" = 3
        ");
                entity.Property(e => e.CompanyId).HasColumnName("CompanyId");
                entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionId");
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.UserName).HasColumnName("UserName");
                entity.Property(e => e.CompanyName).HasColumnName("CompanyName");
                entity.Property(e => e.CompanyLogo).HasColumnName("CompanyLogo");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.StartDate).HasColumnName("StartDate");
                entity.Property(e => e.EndDate).HasColumnName("EndDate");
                entity.Property(e => e.SubscriptionType).HasColumnName("ProductName");
            });
            modelBuilder.Entity<StoresDashboardSummary>(entity =>
            {
                entity.HasNoKey(); 
                entity.ToSqlQuery(@"
        SELECT 
            subs.""SubscriptionId"",
            subs.""CompanyId"",
            subs.""ProductName"",
            comp.""CompanyName"",
            COUNT(prod.""StoreProductId"") as ""ProductCount""
        FROM public.""Subscriptions"" AS subs
        LEFT JOIN public.""Companies"" AS comp
            ON subs.""CompanyId"" = comp.""Id""
        LEFT JOIN public.""StoreFlyer"" AS fly
            ON subs.""SubscriptionId"" = fly.""SubscriptionId""
            AND subs.""CompanyId"" = fly.""CompanyId""
        LEFT JOIN public.""StoreProduct"" AS prod
            ON fly.""StoreFlyersId"" = prod.""FlyerId""
        WHERE subs.""Status"" = 1 
          AND subs.""Vertical"" = 3
          AND subs.""SubVertical"" = 3
        GROUP BY subs.""SubscriptionId"",
                 subs.""CompanyId"",
                 subs.""ProductName"",
                 comp.""CompanyName""
    ");

                entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionId");
                entity.Property(e => e.CompanyId).HasColumnName("CompanyId");
                entity.Property(e => e.SubscriptionType).HasColumnName("ProductName");
                entity.Property(e => e.CompanyName).HasColumnName("CompanyName");
                entity.Property(e => e.ProductCount).HasColumnName("ProductCount");
            });

            base.OnModelCreating(modelBuilder);
        }

    }

}
