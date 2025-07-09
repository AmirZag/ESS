using ESS.Api.Database.Entities.Settings;
using ESS.Api.DTOs.Common;

namespace ESS.Api.DTOs.Settings;

public sealed record AppSettingsDto : ILinkResponse
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public string Value { get; init; }
    public required AppSettingsType Type { get; init; }
    public string? Description { get; init; }
    public required DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; init; }
    public List<LinkDto> Links { get; set; }
}
