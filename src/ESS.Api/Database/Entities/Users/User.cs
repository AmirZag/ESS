namespace ESS.Api.Database.Entities.Users;

public sealed class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string NationalCode { get; set; }
    public string PhoneNumber { get; set; }
    public string PersonalCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string IdentityId { get; set; }
    public string? AvatarKey { get; set; }
}
