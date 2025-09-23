namespace ESS.Api.Database.Entities.Auth;

public sealed class OtpCode
{
    public string Id { get; set; }
    public string PhoneNumber { get; set; }
    public string Code { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}
