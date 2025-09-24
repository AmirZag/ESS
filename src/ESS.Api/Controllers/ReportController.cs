using ESS.Api.Database.DatabaseContext.ApplicationDbContexts;
using ESS.Api.Database.Entities.Settings;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Reports;
using ESS.Api.Helpers;
using ESS.Api.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Controllers;

[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[EnableRateLimiting("default")]
[Authorize(Roles = Roles.Employee)]
[ApiController]
[Route("reports")]
public sealed class ReportController(ApplicationDbContext dbContext, UserContext userContext) : ControllerBase
{

    /// <summary>
    /// Retrieves the payment report for a specific year, month, and level.
    /// </summary>
    /// <param name="reportQuery">Query parameters including Year, Month, and Level.</param>
    /// <returns>The requested payment report file.</returns>
    [HttpGet("payment")]
    public async Task<IActionResult> GetPaymentReport([FromQuery] PaymentReportQuery reportQuery)
    {
        if (reportQuery.Year <= 0 || reportQuery.Month < 1 || reportQuery.Month > 12 || reportQuery.Level <= 0)
        {
            return BadRequest("Invalid query parameters. Year, Month (1-12), and Level must be positive values.");
        }

        string fileNamePattern = $"{await GetPersonalCodeAsync()}-{reportQuery.Year}-{reportQuery.Month}-{reportQuery.Level}";

        return await GetReportFileAsync(AppSettingsKey.PaymentReportImageFolderPath, fileNamePattern, "PaymentReport");
    }

    /// <summary>
    /// Retrieves the personnel file report for a specific year.
    /// </summary>
    /// <param name="reportQuery">Query parameters including Year.</param>
    /// <returns>The requested personnel file report file.</returns>
    [HttpGet("personnel-file")]
    public async Task<IActionResult> GetPersonnelFileReport([FromQuery] PersonnelFileReportQuery reportQuery)
    {
        if (reportQuery.Year <= 0)
        {
            return BadRequest("Invalid query parameters. Year must be positive values.");
        }

        string fileNamePattern = $"h-{await GetPersonalCodeAsync()}-{reportQuery.Year}";

        return await GetReportFileAsync(AppSettingsKey.PersonnelFileImageFolderPath, fileNamePattern, "PersonnelFileReport");
    }

    /// <summary>
    /// Gets the latest Year, Month and Level of the PaymentReport.
    /// </summary>
    /// <returns>The latest Year, Month and Level.</returns>
    [HttpGet("payment/latest")]
    public async Task<IActionResult> GetLatestYearMonthLevel()
    {
        string personalCode = await GetPersonalCodeAsync();
        string folder = await dbContext.AppSettings
            .Where(s => s.Key == AppSettingsKey.PaymentReportImageFolderPath)
            .Select(s => s.Value)
            .FirstOrDefaultAsync() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return Problem(statusCode: 500, detail: "PaymentReport folder not configured or missing.");
        }

        var latest = Directory.GetFiles(folder, $"{personalCode}-*.*")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Select(name =>
            {
                var parts = name.Split('-');
                if (parts.Length < 4)
                {
                    return null;
                }

                return new
                {
                    Year = int.TryParse(parts[1], out int y) ? y : 0,
                    Month = int.TryParse(parts[2], out int m) ? m : 0,
                    Level = int.TryParse(parts[3], out int l) ? l : 0
                };
            })
            .Where(x => x != null && x.Year > 0 && x.Month > 0 && x.Level > 0)
            .OrderByDescending(x => x!.Year)
            .ThenByDescending(x => x!.Month)
            .ThenByDescending(x => x!.Level)
            .FirstOrDefault();

        if (latest == null)
        {
            return NotFound();
        }

        return Ok(latest);
    }

    /// <summary>
    /// Gets the latest Year of the PersonnelFileReport.
    /// </summary>
    /// <returns>The latest Year.</returns>
    [HttpGet("personnel-file/latest")]
    public async Task<IActionResult> GetLatestYear()
    {
        string personalCode = await GetPersonalCodeAsync();
        string folder = await dbContext.AppSettings
            .Where(s => s.Key == AppSettingsKey.PersonnelFileImageFolderPath)
            .Select(s => s.Value)
            .FirstOrDefaultAsync() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return Problem(statusCode: 500, detail: "PersonnelFileReport folder not configured or missing.");
        }

        var latest = Directory.GetFiles(folder, $"h-{personalCode}-*.*")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Select(name =>
            {
                var parts = name.Split('-');
                if (parts.Length < 3)
                {
                    return null;
                }

                return new { Year = int.TryParse(parts[2], out int y) ? y : 0 };
            })
            .Where(x => x != null && x.Year > 0)
            .OrderByDescending(x => x!.Year)
            .FirstOrDefault();

        if (latest == null)
        {
            return NotFound();
        }

        return Ok(latest);
    }

    private async Task<string> GetPersonalCodeAsync()
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        string? personalCode = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.PersonalCode)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(personalCode))
        {
            throw new InvalidOperationException("Personal code not found.");
        }

        return personalCode;
    }
    private async Task<IActionResult> GetReportFileAsync(string settingsKey, string expectedFileNamePattern, string reportType)
    {
        try
        {
            string? imageFolderLocation = await dbContext.AppSettings
                .Where(s => s.Key == settingsKey)
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(imageFolderLocation))
            {
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: $"{reportType} folder path not configured.");
            }

            if (!Directory.Exists(imageFolderLocation))
            {
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: $"{reportType} folder does not exist.");
            }

            string? foundImagePath = FindMatchingFile(imageFolderLocation, expectedFileNamePattern);

            if (string.IsNullOrEmpty(foundImagePath))
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    detail: $"{reportType} not found for the specified parameters: {expectedFileNamePattern}");
            }

            if (!System.IO.File.Exists(foundImagePath))
            {
                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    detail: $"{reportType} file exists in directory but is not accessible.");
            }

            return await ReadAndReturnFileAsync(foundImagePath);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("User not authenticated.");
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                detail: $"Error accessing {reportType.ToLower()} folder: {ex.Message}");
        }
    }
    private string? FindMatchingFile(string folderLocation, string expectedFileNamePattern)
    {
        return Directory.GetFiles(folderLocation)
            .FirstOrDefault(file =>
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file).ToLowerInvariant();

                return fileNameWithoutExtension.Equals(expectedFileNamePattern, StringComparison.OrdinalIgnoreCase) &&
                       FileValidationHelper.AllowedImageExtensions.Contains(extension);
            });
    }
    private async Task<IActionResult> ReadAndReturnFileAsync(string filePath)
    {
        try
        {
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            string fileName = Path.GetFileName(filePath);
            string fileExtension = Path.GetExtension(filePath);

            string contentType = FileValidationHelper.GetContentType(fileExtension);

            return File(fileBytes, contentType, fileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                detail: "Access denied to report file.");
        }
        catch (IOException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                detail: $"Error reading report file: {ex.Message}");
        }
    }
}
