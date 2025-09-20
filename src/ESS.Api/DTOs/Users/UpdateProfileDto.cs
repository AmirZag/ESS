namespace ESS.Api.DTOs.Users;

public sealed record UpdateProfileDto
{
    public string Password { get; init; }
    public string PhoneNumber { get; init; }
}
