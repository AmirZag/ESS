using ESS.Api.Database.Entities.Settings;
using ESS.Api.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Database.DatabaseContext.ApplicationDbContexts;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<AppSettings> AppSettings { get; set; }
    public DbSet<User> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Application);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
