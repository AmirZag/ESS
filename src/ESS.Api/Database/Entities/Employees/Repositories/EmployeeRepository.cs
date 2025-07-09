using ESS.Api.Database.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Database.Entities.Employees.Repositories;

public sealed class EmployeeRepository(IafDbContext dbContext) : IEmployeeRepository
{
    public async Task<Employee?> ValidateEmployee(string nationalCode, string mobile)
    {
        if (string.IsNullOrWhiteSpace(nationalCode) || string.IsNullOrWhiteSpace(mobile))
        {
            return null;
        }

        return await dbContext.EmployeeInfoView
            .FirstOrDefaultAsync(e => e.MelliCode == nationalCode && e.Mobile == mobile);
    }
}
