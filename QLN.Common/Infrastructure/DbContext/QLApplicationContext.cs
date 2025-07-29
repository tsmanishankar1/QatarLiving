using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.DbContext
{
    public class QLApplicationContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public QLApplicationContext(DbContextOptions<QLApplicationContext> options)
            : base(options)
        {
        }
    }
}
