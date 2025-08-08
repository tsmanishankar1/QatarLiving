using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLSubscriptionContext : DbContext
    {
        public QLSubscriptionContext(DbContextOptions<QLSubscriptionContext> options)
           : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<UserAddOn> UserAddOns { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().OwnsOne(
                 p => p.Constraints,
                 ownedNavigationBuilder =>
                 {
                     ownedNavigationBuilder.ToJson();
                 });

            modelBuilder.Entity<Subscription>().OwnsOne(
                s => s.Quota,
                ownedNavigationBuilder =>
                {
                    ownedNavigationBuilder.ToJson();
                });

            base.OnModelCreating(modelBuilder);
        }
    }
}
