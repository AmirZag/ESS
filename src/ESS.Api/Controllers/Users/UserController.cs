using ESS.Api.Database.DatabaseContext;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Users;
using ESS.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Controllers.Users;
[ResponseCache(Duration = 120)]
[EnableRateLimiting("default")]
[Authorize]
[ApiController]
[Route("users")]
public sealed class UserController(ApplicationDbContext dbContext, UserContext userContext) : ControllerBase
{

    /// <summary>
    /// Retrieves a user by their unique ID.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <returns>The requested user information.</returns>
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> GetUsersById(string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (id != userId)
        {
            return Forbid();
        }

        UserDto? user = await dbContext.Users
            .Where(u => u.Id == id)
            .Select(UserQueries.ProjectionToDto())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    /// <summary>
    /// Retrieves the profile of the currently authenticated user.
    /// </summary>
    /// <returns>The current user's profile information.</returns>
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Get current user's profile")]
    [EndpointDescription("Retrieves the profile information for the currently authenticated user.")]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUsers()
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        UserDto? user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(UserQueries.ProjectionToDto())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    /// <summary>
    /// Updates the profile information of the currently authenticated user.
    /// </summary>
    /// <param name="dto">The updated profile data.</param>
    /// <param name="validator"></param>
    /// <param name="userManager"></param>
    /// <returns>No content if the update is successful.</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile(
    UpdateProfileDto dto,
    IValidator<UpdateProfileDto> validator,
    UserManager<IdentityUser> userManager)
    {
        string? userId = await userContext.GetUserIdAsync();
        await validator.ValidateAndThrowAsync(dto);

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user is null)
        {
            return NotFound();
        }

        var identityUser = await userManager.FindByIdAsync(user.IdentityId);
        if (identityUser is null)
        {
            return NotFound();
        }

        bool isChanged = false;

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) 
            && !string.Equals(user.PhoneNumber, dto.PhoneNumber, StringComparison.Ordinal))
        {
            var exists = await dbContext.Users
                .AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId);
            if (exists)
            {
                return Problem(statusCode: 400, detail: "Phone number already in use");
            }

            user.PhoneNumber = dto.PhoneNumber!;
            identityUser.PhoneNumber = dto.PhoneNumber;
            isChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(identityUser);
            var result = await userManager.ResetPasswordAsync(identityUser, token, dto.Password);
            if (!result.Succeeded)
            {
                return Problem(statusCode: 400, detail: "Password update failed");
            }

            isChanged = true;
        }

        if (isChanged)
        {
            user.Touch();
            await dbContext.SaveChangesAsync();
            await userManager.UpdateAsync(identityUser);
        }

        return NoContent();
    }
}
