namespace ESS.Api.DTOs.Auth;

public sealed record TokenRequest(string UserId, string PhoneNumber, IEnumerable<string> Roles);
