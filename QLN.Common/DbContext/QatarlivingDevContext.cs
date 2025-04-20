using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QLN.Common.Model;

namespace QLN.Common.DbContext
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
