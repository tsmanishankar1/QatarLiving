using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLCompanyContext : DbContext
    {
        public QLCompanyContext(DbContextOptions<QLCompanyContext> options)
            : base(options)
        {
        }
        public DbSet<Company> Companies { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Company>();
            entity.Property(c => c.NatureOfBusiness)
                  .HasColumnType("jsonb");

            entity.Property(c => c.BranchLocations)
                  .HasColumnType("jsonb");
            entity.Property(c => c.StartHour)
                  .HasConversion(
                      v => v.HasValue ? v.Value.ToString() : null,
                      v => string.IsNullOrEmpty(v) ? null : TimeSpan.Parse(v)
                  );

            entity.Property(c => c.EndHour)
                  .HasConversion(
                      v => v.HasValue ? v.Value.ToString() : null,
                      v => string.IsNullOrEmpty(v) ? null : TimeSpan.Parse(v)
                  );
            entity.Property(c => c.CRExpiryDate)
           .HasColumnType("timestamp with time zone"); 

            entity.Property(c => c.IsActive)
                  .HasDefaultValue(true);

            entity.Property(c => c.CreatedUtc)
                  .HasDefaultValueSql("now() at time zone 'utc'");
            entity.HasIndex(c => c.UserId);
            entity.HasIndex(c => c.CompanyName);
            base.OnModelCreating(modelBuilder);
        }
    }
}
