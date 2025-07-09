namespace ESS.Api.DTOs.Auth;

public sealed record RegisterUserDto
{
    public required string NationalCode { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Password { get; init; }
}
