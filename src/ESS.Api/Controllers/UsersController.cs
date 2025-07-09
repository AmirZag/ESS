using System.Security.Claims;
using ESS.Api.Database.DatabaseContext;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Users;
using ESS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Controllers;
[ResponseCache(Duration = 120)]
[EnableRateLimiting("default")]
[Authorize]
[ApiController]
[Route("users")]
public sealed class UsersController(ApplicationDbContext dbContext, UserContext userContext) : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> GetUsersById(string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if(id != userId)
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
}
