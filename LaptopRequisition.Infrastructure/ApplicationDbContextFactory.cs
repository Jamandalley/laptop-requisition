using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace LaptopRequisition.Infrastructure
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Build configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../LaptopRequisition.WebAPI")) // Navigate to WebAPI project
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true) // Load development settings
                .Build();

            // Get connection string
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection connection string not found in appsettings.json or appsettings.Development.json.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseMySQL(
                connectionString,
                options =>
                {
                    options.EnableRetryOnFailure();
                });
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}