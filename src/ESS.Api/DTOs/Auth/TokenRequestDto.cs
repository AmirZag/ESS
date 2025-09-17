namespace ESS.Api.DTOs.Auth;

public sealed record TokenRequestDto(string UserId, string PhoneNumber, IEnumerable<string> Roles);
