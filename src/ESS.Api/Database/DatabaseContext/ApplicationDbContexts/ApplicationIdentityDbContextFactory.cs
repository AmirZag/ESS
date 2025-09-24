using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace ESS.Api.Database.DatabaseContext.ApplicationDbContexts;

public class ApplicationIdentityDbContextFactory : IDesignTimeDbContextFactory<ApplicationIdentityDbContext>
{
    public ApplicationIdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationIdentityDbContext>();
        var connectionString = configuration.GetConnectionString("DatabaseConnectionString");

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions
            .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Identity))
            .UseSnakeCaseNamingConvention();

        return new ApplicationIdentityDbContext(optionsBuilder.Options);
    }
}
