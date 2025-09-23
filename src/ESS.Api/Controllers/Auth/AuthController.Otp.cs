using System.Globalization;
using ESS.Api.Database.Entities.Auth;
using ESS.Api.Database.Entities.Token;
using ESS.Api.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Controllers;

public sealed partial class AuthController
{

    /// <summary>
    /// Request OTP code for SMS login
    /// </summary>
    /// <param name="requestOtpDto">Phone number to send OTP</param>
    /// <returns>Success status</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp(RequestOtpDto requestOtpDto)
    {
        var identityUser = await userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == requestOtpDto.PhoneNumber);

        if (identityUser is null)
        {
            return Problem(
                detail: "User with this phone number was not found!",
                statusCode: StatusCodes.Status404NotFound);
        }

        var random = new Random();
        var code = random.Next(100000, 999999).ToString(CultureInfo.InvariantCulture);

        var otpCode = new OtpCode
        {
            Id = $"o_{Guid.CreateVersion7()}",
            PhoneNumber = requestOtpDto.PhoneNumber,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        var existingOtps = await identityDbContext.OtpCodes
            .Where(o => o.PhoneNumber == requestOtpDto.PhoneNumber && !o.IsUsed)
            .ToListAsync();

        identityDbContext.OtpCodes.RemoveRange(existingOtps);
        identityDbContext.OtpCodes.Add(otpCode);
        await identityDbContext.SaveChangesAsync();

        var (success, message) = await smsService.SendVerificationCode(requestOtpDto.PhoneNumber, code);

        if (!success)
        {
            return Problem(
                detail: $"Error sending SMS: {message}",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return Ok(new { Message = "Verification code sent successfully" });
    }

    /// <summary>
    /// Verify OTP code and login
    /// </summary>
    /// <param name="verifyOtpDto">Phone number and OTP code</param>
    /// <returns>Access tokens for the authenticated user</returns>
    [ProducesResponseType(typeof(AccessTokensDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost("verify-otp")]
    public async Task<ActionResult<AccessTokensDto>> VerifyOtp(VerifyOtpDto verifyOtpDto)
    {

        var otpCode = await identityDbContext.OtpCodes
            .FirstOrDefaultAsync(o =>
                o.PhoneNumber == verifyOtpDto.PhoneNumber &&
                o.Code == verifyOtpDto.Code &&
                !o.IsUsed);

        if (otpCode is null)
        {
            return Problem(
                    detail: "Invalid verification code",
                    statusCode: StatusCodes.Status401Unauthorized);
        }

        if (otpCode.ExpiresAt < DateTime.UtcNow)
        {
            return Problem(
                    detail: "Verification code has expired",
                    statusCode: StatusCodes.Status401Unauthorized);
        }

        otpCode.IsUsed = true;
        await identityDbContext.SaveChangesAsync();

        var identityUser = await userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == verifyOtpDto.PhoneNumber);

        if (identityUser is null)
        {
            return Problem(
                    detail: "User not found",
                    statusCode: StatusCodes.Status401Unauthorized);
        }

        IList<string> roles = await userManager.GetRolesAsync(identityUser);

        var tokenRequest = new TokenRequestDto(identityUser.Id, identityUser.PhoneNumber!, roles);
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
}
