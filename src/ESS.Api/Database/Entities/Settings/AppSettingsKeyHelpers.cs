using System.Reflection;

namespace ESS.Api.Database.Entities.Settings;

internal static class AppSettingsKeyHelpers
{
    private static readonly Lazy<HashSet<string>> _validKeys = new Lazy<HashSet<string>>(() =>
        typeof(AppSettingsKey)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => f.GetValue(null))
            .OfType<string>()
            .Where(value => !string.IsNullOrEmpty(value))
            .ToHashSet());
    public static bool IsValid(string key)
    {
        return _validKeys.Value.Contains(key);
    }
}
