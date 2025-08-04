using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

            // Configure one-to-one relationship between ApplicationUser.LegacyUid and UserLegacyData.Uid
            modelBuilder.Entity<ApplicationUser>()
                .HasOne<UserLegacyData>()
                .WithOne()
                .HasForeignKey<ApplicationUser>(u => u.LegacyUid)
                .HasPrincipalKey<UserLegacyData>(l => l.Uid)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserLegacyData>()
                .HasOne<LegacySubscription>()
                .WithOne()
                .HasForeignKey<LegacySubscription>(s => s.Uid)
                .HasPrincipalKey<UserLegacyData>(l => l.Uid)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
