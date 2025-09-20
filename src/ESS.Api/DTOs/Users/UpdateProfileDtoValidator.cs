using FluentValidation;

namespace ESS.Api.DTOs.Users;

public sealed class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileDtoValidator()
    {
        RuleFor(r => r.Password)
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .MaximumLength(64).WithMessage("Password cannot exceed 64 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(r => r.PhoneNumber)
            .Length(11)
            .WithMessage("Length Must be 11")
            .Matches(@"^09\d{9}$").WithMessage("Phone number must start with 09");
    }
}
