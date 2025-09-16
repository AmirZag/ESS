using System.Net.Mime;
using Asp.Versioning;
using ESS.Api.Database.DatabaseContext;
using ESS.Api.Database.Entities.Users;
using ESS.Api.Database.Minio;
using ESS.Api.DTOs.Users.Avatar;
using ESS.Api.Services;
using ESS.Api.Services.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;


namespace ESS.Api.Controllers.Users;

[Authorize(Roles = Roles.Employee)]
[ApiController]
[EnableRateLimiting("default")]
[Route("users/avatar")]
[ApiVersion("1.0")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomeMediaTypeNames.Application.JsonV1,
    CustomeMediaTypeNames.Application.HateoasJson,
    CustomeMediaTypeNames.Application.HateoasJsonV1)]
public sealed class AvatarController(ApplicationDbContext dbContext, 
    UserContext userContext, 
    IMinioService minioService, 
    ILogger<AvatarController> logger) : ControllerBase
{

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
