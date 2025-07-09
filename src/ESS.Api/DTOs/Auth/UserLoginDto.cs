namespace ESS.Api.DTOs.Auth;

public sealed record LoginUserDto
{
    public required string NationalCode { get; init; }
    public required string Password { get; init; }
}
