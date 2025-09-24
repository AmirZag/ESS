using ESS.Api.Database.Entities.Employees;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Database.DatabaseContext.ExternalDbContexts;

public sealed class IafDbContext(DbContextOptions<IafDbContext> options) : DbContext(options)
{
    public DbSet<Employee> EmployeeInfoView { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToView("EmployeeInfoView", "Ess");
            entity.HasNoKey();

            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.PersonalCode).HasColumnName("PersonalCode");
            entity.Property(e => e.MelliCode).HasColumnName("MelliCode");
            entity.Property(e => e.Mobile).HasColumnName("Mobile");
        });
    }
}
