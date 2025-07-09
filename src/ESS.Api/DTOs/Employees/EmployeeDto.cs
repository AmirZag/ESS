namespace ESS.Api.DTOs.Employees;

public record EmployeeDto
{
    public required string Name { get; init; }
    public required string PersonalCode { get; init; }

}
