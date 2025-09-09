using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLApplicationContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<UserCompany> UserCompanies { get; set; }
        public QLApplicationContext(DbContextOptions<QLApplicationContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().OwnsOne(
                    b => b.LegacyData, ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                    });

            modelBuilder.Entity<ApplicationUser>().OwnsOne(
                    b => b.LegacySubscription, ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                    });
            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("UserSubscription");

                entity.HasOne<ApplicationUser>()
                      .WithMany(u => u.Subscriptions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<UserCompany>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("UserCompany");

                entity.HasOne<ApplicationUser>()
                      .WithMany(u => u.Companies)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
