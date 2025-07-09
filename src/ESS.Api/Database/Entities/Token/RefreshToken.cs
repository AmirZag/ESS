using Microsoft.AspNetCore.Identity;

namespace ESS.Api.Database.Entities.Token;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string Token {  get; set; }
    public required DateTime ExpiresAt { get; set; }
    public IdentityUser User { get; set; }
}
