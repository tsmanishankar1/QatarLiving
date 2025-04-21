using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.Model;

namespace QLN.Common.Infrastructure.DbContext
{
    public class QatarlivingDevContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public QatarlivingDevContext(DbContextOptions<QatarlivingDevContext> options)
            : base(options)
        {
        }

        // Optional: other DbSets like Posts, Listings etc.
    }
}
