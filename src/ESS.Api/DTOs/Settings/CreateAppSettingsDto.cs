using System.ComponentModel.DataAnnotations;
using ESS.Api.Database.Entities.Settings;

namespace ESS.Api.DTOs.Settings;

public sealed record CreateAppSettingsDto
{
    public required string Key { get; init; }
    public string Value { get; init; }
    public required AppSettingsType Type { get; init; }
    public string? Description { get; init; }
}
