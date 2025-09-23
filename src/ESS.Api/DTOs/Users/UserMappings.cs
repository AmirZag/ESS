using System.Security.Cryptography;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Auth;
using ESS.Api.DTOs.Employees;

namespace ESS.Api.DTOs.Users;

public static class UserMappings
{
    public static User ToEntity(this RegisterUserDto dto , EmployeeDto employee)
    {
        return new User
        {
            Id = $"u_{Guid.CreateVersion7()}",
            Name = employee.Name,
            NationalCode = dto.NationalCode,
            PhoneNumber = dto.PhoneNumber,
            PersonalCode = employee.PersonalCode,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static void Touch(this User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
    }
}
