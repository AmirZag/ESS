using ESS.Api.Database.Entities.Settings;
using ESS.Api.DTOs.Common;
using ESS.Api.Services.Common;

namespace ESS.Api.DTOs.Settings;

public sealed record AppSettingsQueryParameters : QueryParameter
{
    public AppSettingsType? Type { get; init; }
}
