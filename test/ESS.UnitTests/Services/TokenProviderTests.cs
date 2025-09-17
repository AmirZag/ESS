using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Auth;
using ESS.Api.Services.Common;
using ESS.Api.Setup;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ESS.UnitTests.Services;
public sealed class TokenProviderTests
{
    private readonly TokenProvider _tokenProvider;
    private readonly JwtAuthOptions _jwtAuthOptions;

    public TokenProviderTests()
    {
        IOptions<JwtAuthOptions> options = Options.Create(new JwtAuthOptions()
        {
            Key = "your-secret-key-here-that-should-also-be-fairly-long",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationInMinutes = 30,
            RefreshTokenExpirationDays = 7,
        });

        _jwtAuthOptions = options.Value;
        _tokenProvider = new(options);
    }

    [Fact]
    public void Create_ShouldReturnAccessTokens()
    {
        // Arrange
        TokenRequestDto dto = new(
             User.CreateNewId(),
            "test@example.com",
            [Roles.Employee]
       );

        // Act
        AccessTokensDto accessTokensDto = _tokenProvider.Create(dto);

        // Assert
        Assert.NotNull(accessTokensDto.AccessToken);
        Assert.NotNull(accessTokensDto.RefreshToken);
    }

    [Fact]
    public void Create_ShouldGenerateValidAccessToken()
    {
        // Arrange
        TokenRequestDto dto = new(
             User.CreateNewId(),
            "test@example.com",
            [Roles.Employee]
       );

        // Act
        AccessTokensDto accessTokensDto = _tokenProvider.Create(dto);

        // Assert
        JwtSecurityTokenHandler handler = new()
        {
            MapInboundClaims = false,
        };

        TokenValidationParameters validationParameters = new()
        {
            ValidIssuer = _jwtAuthOptions.Issuer,
            ValidAudience = _jwtAuthOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtAuthOptions.Key)),
            ValidateIssuerSigningKey = true,
            NameClaimType = JwtRegisteredClaimNames.Email,
        };

        ClaimsPrincipal claimsPrincipal = handler.ValidateToken(
            accessTokensDto.AccessToken,
            validationParameters,
            out SecurityToken validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal(dto.UserId, claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub));
        Assert.Equal(dto.PhoneNumber, claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.PhoneNumber));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueRefreshTokens()
    {
        // Arrange
        TokenRequestDto dto = new(
             User.CreateNewId(),
            "test@example.com",
            [Roles.Employee]
       );

        // Act
        AccessTokensDto accessTokensDto1 = _tokenProvider.Create(dto);
        AccessTokensDto accessTokensDto2 = _tokenProvider.Create(dto);

        // Assert
        Assert.NotEqual(accessTokensDto1.RefreshToken, accessTokensDto2.RefreshToken);
    }

    [Fact]
    public void Create_ShouldGenerateAccessTokenWithCorrectExpiration()
    {
        // Arrange
        TokenRequestDto dto = new(
             User.CreateNewId(),
            "test@example.com",
            [Roles.Employee]
       );

        // Act
        AccessTokensDto accessTokensDto = _tokenProvider.Create(dto);

        // Assert
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken jwtSecurityToken = handler.ReadJwtToken(accessTokensDto.AccessToken);

        DateTime expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtAuthOptions.ExpirationInMinutes);
        DateTime actualExpiration = jwtSecurityToken.ValidTo;

        // Allow for a small time difference due to test execution
        Assert.True(Math.Abs((expectedExpiration - actualExpiration).TotalSeconds) < 3);
    }

    [Fact]
    public void Create_ShouldGenerateBase64RefreshToken()
    {
        // Arrange
        TokenRequestDto dto = new(
             User.CreateNewId(),
            "test@example.com",
            [Roles.Employee]
       );

        // Act
        AccessTokensDto accessTokensDto = _tokenProvider.Create(dto);

        // Assert
        Assert.True(IsBase64String(accessTokensDto.RefreshToken));
    }

    private static bool IsBase64String(string base64)
    {
        Span<byte> buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64, buffer, out _);
    }
}
