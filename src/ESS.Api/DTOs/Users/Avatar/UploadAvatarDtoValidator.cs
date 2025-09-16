using ESS.Api.Options;
using FluentValidation;
using Microsoft.EntityFrameworkCore;


namespace ESS.Api.DTOs.Users.Avatar;

public sealed class UploadAvatarDtoValidator : AbstractValidator<UploadAvatarDto>
{
    private const int MaxFileSizeInMB = 10;
    private const long MaxFileSizeInBytes = MaxFileSizeInMB * 1024 * 1024;

    public UploadAvatarDtoValidator()
    {
        RuleFor(x => x.Avatar)
            .NotNull().WithMessage("Avatar file is required")
            .Must(BeValidSize).WithMessage($"File size must not exceed {MaxFileSizeInMB}MB")
            .Must(BeValidFileType).WithMessage("File must be a valid image format (jpg, jpeg, png, webp, gif)")
            .Must(HaveValidExtension).WithMessage("File extension is not allowed");
    }

    private bool BeValidSize(IFormFile file)
    {
        return file?.Length > 0 && file.Length <= MaxFileSizeInBytes;
    }

    private bool BeValidFileType(IFormFile file)
    {
        if (file == null)
        {
            return false;
        }

        return FileValidationOptions.AllowedImageContentTypes.Contains(
            file.ContentType.ToLowerInvariant());
    }

    private bool HaveValidExtension(IFormFile file)
    {
        if (file == null)
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return FileValidationOptions.AllowedImageExtensions.Contains(extension);
    }
}
