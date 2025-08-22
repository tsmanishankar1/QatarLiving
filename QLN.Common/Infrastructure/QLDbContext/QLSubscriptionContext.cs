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
            modelBuilder.Entity<Product>(entity =>
            {
                entity.OwnsOne(p => p.Constraints, constraintsBuilder =>
                {
                    constraintsBuilder.ToJson();

                    constraintsBuilder.OwnsMany(c => c.CategoryQuotas, categoryQuotaBuilder =>
                    {
                        categoryQuotaBuilder.Property(cq => cq.Category).IsRequired();
                        categoryQuotaBuilder.Property(cq => cq.AdsBudget).IsRequired();
                    });
                });
            });

            modelBuilder.Entity<Subscription>().OwnsOne(s => s.Quota, qb =>
            {
                qb.ToJson();
                qb.OwnsMany(q => q.CategoryQuotas, cq =>
                {
                    cq.ToJson();
                });
                qb.OwnsOne(q => q.SocialMedia, sm => { sm.ToJson(); });
            });

            modelBuilder.Entity<UserAddOn>().OwnsOne(a => a.Quota, qb =>
            {
                qb.ToJson();
                qb.OwnsMany(q => q.CategoryQuotas, cq =>
                {
                    cq.ToJson();
                });
                qb.OwnsOne(q => q.SocialMedia, sm => { sm.ToJson(); });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
