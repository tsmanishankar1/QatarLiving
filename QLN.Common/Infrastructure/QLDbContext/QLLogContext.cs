using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLLogContext : DbContext
    {
        public QLLogContext(DbContextOptions<QLLogContext> options)
            : base(options)
        {
        }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Payload)
                .HasColumnType("jsonb");

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.CreatedUtc)
                .HasDefaultValueSql("now() at time zone 'utc'");

            modelBuilder.Entity<ErrorLog>()
                .Property(e => e.CreatedUtc)
                .HasDefaultValueSql("now() at time zone 'utc'");

            base.OnModelCreating(modelBuilder);
        }
    }
}
