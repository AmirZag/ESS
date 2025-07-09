using System.Globalization;
using ESS.Api.Database.Entities.Employees;

namespace ESS.Api.DTOs.Employees;

public static class EmployeeMappings
{
    public static EmployeeDto ToDto(this Employee employee)
    {
        return new EmployeeDto
        {
            Name = employee.Name,
            PersonalCode = employee.PersonalCode.ToString(CultureInfo.InvariantCulture),
        };
    }
}
