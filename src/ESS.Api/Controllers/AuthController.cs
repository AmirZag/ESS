using ESS.Api.Database.DatabaseContext;
using ESS.Api.Database.Entities.Employees;
using ESS.Api.Database.Entities.Employees.Repositories;
using ESS.Api.Database.Entities.Token;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Auth;
using ESS.Api.DTOs.Employees;
using ESS.Api.DTOs.Users;
using ESS.Api.Services.Common;
using ESS.Api.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace ESS.Api.Controllers;

[ApiController]
[EnableRateLimiting("default")]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    ApplicationDbContext applicationDbContext,
    ApplicationIdentityDbContext identityDbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> options
    ) : ControllerBase
{
    private readonly JwtAuthOptions _jwtAuthOptions = options.Value;

    [HttpPost("register")]
    public async Task<ActionResult<AccessTokensDto>> Register(IEmployeeRepository employeeRepository, RegisterUserDto registerUserDto)
    {

        Employee employee = await employeeRepository.ValidateEmployee(registerUserDto.NationalCode, registerUserDto.PhoneNumber);

        if (employee is null)
        {
            return Problem(
                detail: "Unable to register, User is not Valid!",
                statusCode: StatusCodes.Status400BadRequest);
        }

        EmployeeDto employeeDto = employee.ToDto();

        using IDbContextTransaction transaction = await identityDbContext.Database.BeginTransactionAsync();
        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        var identityUser = new IdentityUser
        {
            UserName = registerUserDto.NationalCode,
            PhoneNumber = registerUserDto.PhoneNumber,
        };

        IdentityResult createUserResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!createUserResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>
            {
                {
                    "error",
                    createUserResult.Errors.ToDictionary(e => e.Code , e => e.Description)
                }
            };

            return Problem(
                detail: "Unable to register user, Please try again!",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        IdentityResult addToRoleAsync = await userManager.AddToRoleAsync(identityUser, Roles.Employee);

        if (!addToRoleAsync.Succeeded)
        {
            var extensions = new Dictionary<string, object?>
            {
                {
                    "error",
                    addToRoleAsync.Errors.ToDictionary(e => e.Code , e => e.Description)
                }
            };

            return Problem(
                detail: "Unable to register user, Please try again!",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        User user = registerUserDto.ToEntity(employeeDto);
        user.IdentityId = identityUser.Id;

        applicationDbContext.Users.Add(user);
        await applicationDbContext.SaveChangesAsync();

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.PhoneNumber , [Roles.Employee]);
        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays),
        };
        identityDbContext.RefreshTokens.Add(refreshToken);

        await identityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(accessTokens);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccessTokensDto>> Login(LoginUserDto loginUserDto)
    {
        #region 2FA Authentication
        //Implementation
        #endregion
        IdentityUser? identityUser = await userManager.FindByNameAsync(loginUserDto.NationalCode);

        if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser , loginUserDto.Password))
        {
            return Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(identityUser);

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.PhoneNumber!, roles);
        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays),
        };
        identityDbContext.RefreshTokens.Add(refreshToken);

        await identityDbContext.SaveChangesAsync();

        return Ok(accessTokens);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AccessTokensDto>> Refresh(RefreshTokenDto refreshTokenDto)
    {
        RefreshToken? refreshToken = await identityDbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshTokenDto.RefreshToken);

        if (refreshToken is null)
        {
            return Unauthorized();

        }
        if (refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(refreshToken.User);

        var tokenRequest = new TokenRequest(refreshToken.User.Id, refreshToken.User.PhoneNumber!, roles);
        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);

        refreshToken.Token = accessTokens.RefreshToken;
        refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays);

        await identityDbContext.SaveChangesAsync();

        return Ok(accessTokens);
    }
}
