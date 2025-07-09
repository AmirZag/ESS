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

    /// <summary>
    /// We'll use this to store the user's identityID from the Identity Provider.
    /// This Could be any Identity Provider like Azure AD, Okta, Auth0, etc.
    /// </summary>
    public string IdentityId { get; set; }
}
