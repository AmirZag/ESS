using System.Linq.Expressions;
using ESS.Api.Database.Entities.Users;

namespace ESS.Api.DTOs.Users;

internal static class UserQueries
{
    public static Expression<Func<User, UserDto>> ProjectionToDto()
    {
        return user => new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            NationalCode = user.NationalCode,
            PhoneNumber = user.PhoneNumber,
            PersonalCode = user.PersonalCode,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            AvatarUrl = user.AvatarKey,
        };

    }
}
