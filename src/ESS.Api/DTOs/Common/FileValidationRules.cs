namespace ESS.Api.DTOs.Common;

public static class FileValidationRules
{
    public static readonly string[] AllowedImageExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public static readonly string[] AllowedImageContentTypes =
        { "image/jpeg", "image/png", "image/webp", "image/gif" };
}

