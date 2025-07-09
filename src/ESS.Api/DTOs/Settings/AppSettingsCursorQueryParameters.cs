using ESS.Api.Database.Entities.Settings;
using ESS.Api.DTOs.Common;
using Microsoft.AspNetCore.Mvc;

namespace ESS.Api.DTOs.Settings;

public sealed record AppSettingsCursorQueryParameters : AcceptHeaderDto
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Cursor { get; init; }
    public string? Fields { get; init; }
    public int Limit { get; init; } = 10;
    public AppSettingsType? Type { get; init; }
}
