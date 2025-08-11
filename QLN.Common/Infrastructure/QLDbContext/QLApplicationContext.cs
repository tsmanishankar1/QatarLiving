using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLApplicationContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
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
        }
    }
}
