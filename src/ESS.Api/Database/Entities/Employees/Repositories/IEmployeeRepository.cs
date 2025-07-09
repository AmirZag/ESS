namespace ESS.Api.Database.Entities.Employees.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> ValidateEmployee(string nationalCode, string mobile);
}
