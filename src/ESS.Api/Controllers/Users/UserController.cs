using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using ESS.Api.Database.DatabaseContext;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Common;
using ESS.Api.DTOs.Users;
using ESS.Api.Services;
using ESS.Api.Services.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ESS.Api.Controllers.Users;
[ResponseCache(Duration = 120)]
[EnableRateLimiting("default")]
[Authorize]
[ApiController]
[Route("users")]
public sealed class UserController(ApplicationDbContext dbContext, UserContext userContext) : ControllerBase
{
    [EndpointSummary("Get a user by ID")]
    [EndpointDescription("Retrieves a user by their unique identifier. This endpoint requires Admin role permissions.")]
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

    [HttpPut("me/profile")]
    [EndpointSummary("Update current user's profile")]
    [EndpointDescription("Updates the profile information for the currently authenticated user.")]
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

        if (!string.Equals(user.PhoneNumber, dto.PhoneNumber, StringComparison.Ordinal))
        {
            var exists = await dbContext.Users
                .AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId);
            if (exists)
            {
                return Problem(statusCode: 400, detail: "Phone number already in use");
            }

            user.PhoneNumber = dto.PhoneNumber;
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
            user.UpdateFromDto();
            await dbContext.SaveChangesAsync();
            await userManager.UpdateAsync(identityUser);
        }

        return NoContent();
    }
}
