using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s.ClassifiedsBo;
using System.Reflection.Metadata;

namespace QLN.Classified.MS.DBContext
{
    public class ClassifiedDevContext : DbContext
    {
        public ClassifiedDevContext(DbContextOptions<ClassifiedDevContext> options)
            : base(options)
        {
        }

        public DbSet<StoresDto> storesDtos { get; set; }
        public DbSet<StoresSubscriptionDto> StoresSubscriptions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
