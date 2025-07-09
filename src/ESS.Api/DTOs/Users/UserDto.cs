namespace ESS.Api.DTOs.Users;

public sealed class UserDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string NationalCode { get; set; }
    public required string PhoneNumber { get; set; }
    public required string PersonalCode { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
