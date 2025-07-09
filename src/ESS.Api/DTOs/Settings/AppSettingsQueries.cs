using System.Linq.Expressions;
using ESS.Api.Database.Entities.Settings;

namespace ESS.Api.DTOs.Settings;

internal static class AppSettingsQueries
{
    public static Expression<Func<AppSettings, AppSettingsDto>> ProjectToDto()
    {
        return s => new AppSettingsDto
        {
            Id = s.Id,
            Key = s.Key,
            Value = s.Value,
            Type = s.Type,
            Description = s.Description,
            CreatedAt = s.CreatedAt,
            ModifiedAt = s.ModifiedAt
        };
    }
}
