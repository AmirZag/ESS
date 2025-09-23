using System.Collections.Frozen;

namespace ESS.Api.Helpers;

public static class FileValidationHelper
{
    public static readonly string[] AllowedImageExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public static readonly string[] AllowedImageContentTypes =
        { "image/jpeg", "image/png", "image/webp", "image/gif" };

    private static readonly FrozenDictionary<string, string> ExtensionToContentTypeMap =
        new Dictionary<string, string>
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".webp", "image/webp" },
            { ".gif", "image/gif" }
        }.ToFrozenDictionary();

    public static string GetContentType(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return "application/octet-stream";
        }

        var normalizedExtension = extension.ToLowerInvariant();
        return ExtensionToContentTypeMap.TryGetValue(normalizedExtension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
