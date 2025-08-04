using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using QLN.Common.Infrastructure.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common
{
    public class ClassifiedDevContextFactory : IDesignTimeDbContextFactory<ClassifiedDevContext>
    {
        public ClassifiedDevContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../QLN.BackEndAPI");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine(connectionString);
            var optionsBuilder = new DbContextOptionsBuilder<ClassifiedDevContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new ClassifiedDevContext(optionsBuilder.Options);
        }
    }
}
