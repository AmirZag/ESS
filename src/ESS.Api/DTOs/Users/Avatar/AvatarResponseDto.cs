namespace ESS.Api.DTOs.Users.Avatar;

public sealed class AvatarResponseDto
{
    public string AvatarKey { get; set; } = null!;
    public string AvatarUrl { get; set; } = null!;
    public DateTime UploadedAtUtc { get; set; }
}
