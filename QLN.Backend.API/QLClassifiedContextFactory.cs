//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.Extensions.Configuration;
//using QLN.Common.Infrastructure.QLDbContext;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace QLN.Backend.API
//{
//    public class QLClassifiedContextFactory : IDesignTimeDbContextFactory<QLClassifiedContext>
//    {
//        public QLClassifiedContext CreateDbContext(string[] args)
//        {
//            var basePath = Path.Combine(Directory.GetCurrentDirectory());
//            var configuration = new ConfigurationBuilder()
//                .SetBasePath(basePath)
//                .AddJsonFile("appsettings.Development.json", optional: true)
//                .AddJsonFile("appsettings.json", optional: false)
//                .Build();

//            var connectionString = configuration.GetConnectionString("DefaultConnection");
//            Console.WriteLine(connectionString);
//            var optionsBuilder = new DbContextOptionsBuilder<QLClassifiedContext>();
//            optionsBuilder.UseNpgsql(connectionString);

//            return new QLClassifiedContext(optionsBuilder.Options);
//        }
//    }
//}
