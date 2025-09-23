using System.Net.Mime;
using Asp.Versioning;
using ESS.Api.Database.DatabaseContext;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Users.Avatar;
using ESS.Api.Helpers;
using ESS.Api.Services;
using ESS.Api.Services.Common.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;


namespace ESS.Api.Controllers.Users;

[Authorize(Roles = Roles.Employee)]
[ApiController]
[EnableRateLimiting("default")]
[Route("users/me/avatar")]
[ApiVersion("1.0")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomeMediaTypeNames.Application.JsonV1,
    CustomeMediaTypeNames.Application.HateoasJson,
    CustomeMediaTypeNames.Application.HateoasJsonV1)]
public sealed class AvatarController(ApplicationDbContext dbContext, 
    UserContext userContext, 
    IFileService minioService, 
    ILogger<AvatarController> logger) : ControllerBase
{

    /// <summary>
    /// Uploads a new avatar for the current user.
    /// </summary>
    /// <param name="uploadDto">The avatar file to upload.</param>
    /// <param name="validator"></param>
    /// <returns>The uploaded avatar information.</returns>
    [ProducesResponseType(typeof(AvatarResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost]
    public async Task<ActionResult<AvatarResponseDto>> UploadAvatar(
        UploadAvatarDto uploadDto,
        IValidator<UploadAvatarDto> validator)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var validationResult = await validator.ValidateAsync(uploadDto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var user = await dbContext.Users.FindAsync(userId);
        if (user is null)
        {
            return NotFound("User not found");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(user.AvatarKey))
            {
                await minioService.DeleteFileAsync(user.AvatarKey);
            }

            var fileExtension = Path.GetExtension(uploadDto.Avatar.FileName);
            var fileName = $"avatars/{userId}/{Guid.CreateVersion7()}{fileExtension}";

            using var stream = uploadDto.Avatar.OpenReadStream();
            var uploadResult = await minioService.UploadFileAsync(
                stream,
                fileName,
                uploadDto.Avatar.ContentType);

            user.AvatarKey = uploadResult.ObjectName;
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            var presignedUrl = await minioService.GetPresignedUrlAsync(user.AvatarKey);

            return Ok(new AvatarResponseDto
            {
                AvatarKey = user.AvatarKey,
                AvatarUrl = presignedUrl,
                UploadedAtUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading avatar for user {UserId}", userId);
            return Problem(
                detail: "An error occurred while uploading the avatar",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retrieves the current user's avatar.
    /// </summary>
    /// <returns>The avatar file.</returns>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    public async Task<IActionResult> GetAvatar()
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.AvatarKey)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(user))
        {
            return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    detail: $"Avatar Not Found");
        }

        try
        {
            var (stream, contentType, fileName) = await minioService.GetObjectAsync(user);
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving avatar for user {UserId}", userId);
            return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    detail: $"Avatar Not Found");
        }
    }

    /// <summary>
    /// Deletes the current user's avatar.
    /// </summary>
    /// <returns>No content if deletion is successful.</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete]
    public async Task<IActionResult> DeleteAvatar()
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.FindAsync(userId);
        if (user is null || string.IsNullOrWhiteSpace(user.AvatarKey))
        {
            return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    detail: $"Avatar Not Found");
        }

        try
        {
            await minioService.DeleteFileAsync(user.AvatarKey);

            user.AvatarKey = null;
            user.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting avatar for user {UserId}", userId);
            return Problem(
                detail: "An error occurred while deleting the avatar",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
