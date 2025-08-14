using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.QLDbContext
{
    public class QLNotificationContext : DbContext
    {
        public QLNotificationContext(DbContextOptions<QLNotificationContext> options)
            : base(options)
        {
        }
        public DbSet<NotificationEntity> Notifications { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NotificationEntity>()
                .Property(n => n.Plaintext)
                .IsRequired();
            modelBuilder.Entity<NotificationEntity>()
                .Property(n => n.Subject)
                .IsRequired();
            modelBuilder.Entity<NotificationEntity>()
                .Property(n => n.Html)
                .IsRequired(false);
            modelBuilder.Entity<AttachmentDto>().HasNoKey();
            base.OnModelCreating(modelBuilder);
        }
    }
}
