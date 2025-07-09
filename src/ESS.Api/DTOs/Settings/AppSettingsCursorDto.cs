using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace ESS.Api.DTOs.Settings;

public sealed record AppSettingsCursorDto(string Id)
{
    public static string Encode(string id)
    {
        var cursor = new AppSettingsCursorDto(id);
        string json = JsonSerializer.Serialize(cursor);
        return Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
    }

    public static AppSettingsCursorDto? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            string json = Base64UrlEncoder.Decode(cursor);
            return JsonSerializer.Deserialize<AppSettingsCursorDto>(json);
        }
        catch
        {
            return null;
        }
    }
}
