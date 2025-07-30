using Amazon.S3.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s.Payments;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLPaymentsContext : DbContext
    {
        public DbSet<D365PaymentLogsEntity> D365PaymentLogs { get; set; }

        public DbSet<D365RequestsLogsEntity> D365RequestsLogs { get; set; }

        public DbSet<PaymentEntity> Payments { get; set; }

        public DbSet<D365LookupEntity> D365Lookups { get; set; }

        public QLPaymentsContext(DbContextOptions<QLPaymentsContext> options)
            : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<D365PaymentLogsEntity>().OwnsOne(
                    b => b.Response, ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                        ownedNavigationBuilder.OwnsOne(response => response);
                    });

            modelBuilder.Entity<D365RequestsLogsEntity>().OwnsOne(
                    b => b.Payload, ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                        ownedNavigationBuilder.OwnsOne(payload => payload);
                    });

            modelBuilder.Entity<D365RequestsLogsEntity>().OwnsOne(
                    b => b.Response, ownedNavigationBuilder =>
                    {
                        ownedNavigationBuilder.ToJson();
                        ownedNavigationBuilder.OwnsOne(response => response);
                    });
        }

    }
}
