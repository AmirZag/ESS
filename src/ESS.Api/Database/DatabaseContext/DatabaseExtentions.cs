using ESS.Api.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Database.DatabaseContext;

public static class DatabaseExtentions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
       using IServiceScope scope = app.Services.CreateScope();
       await using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
       await using ApplicationIdentityDbContext identityDbContext = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Application database migrations applied successfully. ");

            await identityDbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Identity database migrations applied successfully. ");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An Error occurred while applying database migrations. ");
            throw;
        }
    }

    public static async Task SeedInitialDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = app.Logger;

        try
        {

            if (!await roleManager.RoleExistsAsync(Roles.Employee))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Employee));
            }

            if (!await roleManager.RoleExistsAsync(Roles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            }

            var config = app.Configuration.GetSection("SeedAdmin");
            string adminNationalCode = config["NationalCode"];
            string adminPhone = config["PhoneNumber"];
            string adminPassword = config["Password"];
            string adminName = config["Name"];
            string personalCode = config["PersonalCode"];


            var existingUser = await userManager.FindByNameAsync(adminNationalCode!);
            if (existingUser == null)
            {
                var identityUser = new IdentityUser
                {
                    UserName = adminNationalCode,
                    PhoneNumber = adminPhone
                };

                var createResult = await userManager.CreateAsync(identityUser, adminPassword!);
                if (!createResult.Succeeded)
                {
                    throw new Exception("Failed to create admin IdentityUser: " +
                        string.Join(";", createResult.Errors.Select(e => e.Description)));
                }

                var roleResult = await userManager.AddToRoleAsync(identityUser, Roles.Admin);
                if (!roleResult.Succeeded)
                {
                    throw new Exception("Failed to add admin role: " +
                        string.Join(";", roleResult.Errors.Select(e => e.Description)));
                }

                var adminUser = new User
                {
                    Id = $"u_{Guid.CreateVersion7()}",
                    Name = adminName!,
                    NationalCode = adminNationalCode!,
                    PhoneNumber = adminPhone!,
                    PersonalCode = personalCode!,
                    CreatedAt = DateTime.UtcNow,
                    IdentityId = identityUser.Id
                };

                applicationDbContext.Users.Add(adminUser);
                await applicationDbContext.SaveChangesAsync();

                logger.LogInformation("Admin user seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding initial data.");
            throw;
        }
    }

}
