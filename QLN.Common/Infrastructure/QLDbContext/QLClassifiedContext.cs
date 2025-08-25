using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Identity.Client;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsFo;
using QLN.Common.Infrastructure.Model;
using System.Text.Json;



namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLClassifiedContext : DbContext
    {
        public QLClassifiedContext(DbContextOptions<QLClassifiedContext> options)
            : base(options)
        {
        }

        public DbSet<SaveSearch> saveSearches { get; set; }
        public DbSet<StoresSubscriptionDto> StoresSubscriptions { get; set; }
        public DbSet<ViewStoresSubscription> ViewStoresSubscriptions { get; set; }
        public DbSet<StoreStatus> StoreStatuses { get; set; }
        public DbSet<StoreProducts> StoreProduct { get; set; }
        public DbSet<ProductFeatures> ProductFeature { get; set; }
        public DbSet<ProductImages> ProductImage { get; set; }
        public DbSet<Items> Item { get; set; }
        public DbSet<Deals> Deal { get; set; }
        public DbSet<Preloveds> Preloved { get; set; }
        public DbSet<Collectibles> Collectible { get; set; }
        public DbSet<StoreFlyers> StoreFlyer { get; set; }
        public DbSet<SeasonalPicks> SeasonalPicks { get; set; }
        public DbSet<CategoryDropdown> CategoryDropdowns { get; set; }
        public DbSet<FeaturedStore> FeaturedStores { get; set; }
        public DbSet<FeaturedCategory> FeaturedCategories { get; set; }
        public DbSet<Services> Services { get; set; }
        public DbSet<StoresDashboardHeader> StoresDashboardHeaderItems { get; set; }
        public DbSet<StoresDashboardSummary> StoresDashboardSummaryItems { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<StoreCompanyDto> StoreCompanyDto { get; set; }
        public DbSet<StoreSubscriptionQuotaDto> StoreSubscriptionQuotaDtos { get; set; }
        public DbSet<Comment> Comments { get; set; }
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

            modelBuilder.Entity<CategoryDropdown>()
            .Property(c => c.Fields)
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
            modelBuilder.Entity<CategoryDropdown>()
            .Property(c => c.Fields)
           .HasColumnType("jsonb");

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
                comp.""CompanyVerificationStatus"",
                comp.""CompanyName"",
                comp.""CompanyLogo"",
                subs.""StartDate"",
                subs.""EndDate"",
                '' as ""XMLFeed"",
            '' as ""UploadFeed""
            FROM public.""Subscriptions"" AS subs
            INNER JOIN public.""Companies"" AS comp
                ON subs.""CompanyId"" = comp.""Id""
            WHERE
              subs.""Vertical"" = 3
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
        INNER JOIN public.""Companies"" AS comp
            ON subs.""CompanyId"" = comp.""Id""
        LEFT JOIN public.""StoreFlyer"" AS fly
            ON subs.""SubscriptionId"" = fly.""SubscriptionId""
            AND subs.""CompanyId"" = fly.""CompanyId""
        LEFT JOIN public.""StoreProduct"" AS prod
            ON fly.""StoreFlyersId"" = prod.""FlyerId""
        WHERE subs.""Vertical"" = 3
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
            modelBuilder.Entity<ViewStoresSubscription>(entity =>
            {
                entity.HasNoKey();
                entity.ToSqlQuery(@"
        SELECT comp.""Id"" as ""CompanyId"",
               subs.""SubscriptionId"",
               subs.""ProductName"" as ""SubscriptionType"",
               subs.""UserId"",
               comp.""UserName"",
               comp.""PhoneNumber"" as ""Mobile"",
               comp.""WhatsAppNumber"" as ""Whatsapp"",
               comp.""WebsiteUrl"" as ""WebUrl"",
               comp.""Email"",
               comp.""Status"",
               comp.""CompanyName"",
               subs.""StartDate"",
               subs.""EndDate"",
               pay.""PaymentId"" as ""OrderId"",
               pay.""Fee"" as ""Amount""
        FROM public.""Subscriptions"" AS subs
        INNER JOIN public.""Companies"" AS comp
            ON subs.""CompanyId"" = comp.""Id""
        INNER JOIN public.""Payments"" AS pay
            ON subs.""PaymentId"" = pay.""PaymentId""
        WHERE subs.""Vertical"" = 3
          AND subs.""SubVertical"" = 3
    ");

                entity.Property(e => e.CompanyId).HasColumnName("CompanyId");
                entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionId");
                entity.Property(e => e.SubscriptionType).HasColumnName("SubscriptionType");
                entity.Property(e => e.UserId).HasColumnName("UserId");
                entity.Property(e => e.UserName).HasColumnName("UserName");
                entity.Property(e => e.Mobile).HasColumnName("Mobile");
                entity.Property(e => e.Whatsapp).HasColumnName("Whatsapp");
                entity.Property(e => e.WebUrl).HasColumnName("WebUrl");
                entity.Property(e => e.Email).HasColumnName("Email");
                entity.Property(e => e.Status).HasColumnName("Status");
                entity.Property(e => e.CompanyName).HasColumnName("CompanyName");
                entity.Property(e => e.StartDate).HasColumnName("StartDate");
                entity.Property(e => e.EndDate).HasColumnName("EndDate");
                entity.Property(e => e.OrderId).HasColumnName("OrderId");
                entity.Property(e => e.Amount).HasColumnName("Amount");
            });
 
            modelBuilder.Entity<StoreCompanyDto>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.BranchLocations)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()), 
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)               
                    )
                    .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));

                entity.ToSqlQuery(@"
        SELECT 
            ""Id"",
            ""CompanyName"",
            ""CompanyLogo"",
            ""CoverImage1"",
            ""PhoneNumber"",
            ""Email"",
            ""WebsiteUrl"",
            ""BranchLocations"",
            ""Slug""
        FROM public.""Companies""
    ");
            });

            modelBuilder.Entity<StoreSubscriptionQuotaDto>(entity =>
            {
                entity.HasNoKey();

                entity.ToSqlQuery(@"
        SELECT 
            ""SubscriptionId"", 
            ""Quota""
        FROM public.""Subscriptions""
        WHERE ""Vertical"" = 3 AND ""SubVertical"" = 3
    ");

                entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionId");
                entity.Property(e => e.QuotaJson).HasColumnName("Quota");
            });

            base.OnModelCreating(modelBuilder);
        }

    }

}
