using System.Dynamic;
using System.Net.Mime;
using Asp.Versioning;
using ESS.Api.Database.DatabaseContext.ApplicationDbContexts;
using ESS.Api.Database.Entities.Settings;
using ESS.Api.Database.Entities.Users;
using ESS.Api.DTOs.Common;
using ESS.Api.DTOs.Settings;
using ESS.Api.Helpers;
using ESS.Api.Services.Common;
using ESS.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ESS.Api.Controllers.Common;
[EnableRateLimiting("default")]
[Authorize(Roles = Roles.Admin)]
[ApiController]
[Route("settings")]
[ApiVersion("1.0")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomeMediaTypeNames.Application.HateoasJson,
    CustomeMediaTypeNames.Application.HateoasJsonV1,
    CustomeMediaTypeNames.Application.JsonV1)]
public sealed class AppSettingsController(
    ApplicationDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{

    /// <summary>
    /// Retrieves a paginated list of application settings.
    /// </summary>
    /// <param name="query">Query parameters for filtering, sorting, paging, and shaping.</param>
    /// <param name="sortMappingProvider"></param>
    /// <param name="dataShapingService"></param>
    /// <returns>Paginated list of application settings.</returns>
    [ResponseCache(Duration = 120)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginationResult<ExpandoObject>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<IActionResult> GetAppSettings(
        [FromQuery] AppSettingsQueryParameters query,
        SortMappingProvider sortMappingProvider,
        DataShapingService dataShapingService)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!sortMappingProvider.ValidateMappings<AppSettingsDto, AppSettings>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter isn't valid: '{query.Sort}'");
        }

        if (!dataShapingService.Validate<AppSettingsDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided Fields aren't valid: '{query.Fields}'");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            query.Search = query.Search.Trim().ToLower();
        }

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<AppSettingsDto, AppSettings>();

        IQueryable<AppSettingsDto> appSettingsQuery = dbContext
            .AppSettings
            .Where(s => query.Search == null || s.Key.ToLower().Contains(query.Search))
            .Where(s => query.Type == null || s.Type == query.Type)
            .ApplySort(query.Sort, sortMappings)
            .Select(AppSettingsQueries.ProjectToDto());

        int totalCount = await appSettingsQuery.CountAsync();

        var appSettings = await appSettingsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(
                appSettings,
                query.Fields,
                query.IncludesLinks ? s => CreateLinksForAppSettings(s.Id, query.Fields) : null),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
        if (query.IncludesLinks)
        {
            paginationResult.Links = CreateLinksForAppSettings(
                    query,
                    paginationResult.HasNextPage,
                    paginationResult.HasPreviousPage);
        }
        return Ok(paginationResult);
    }

    /// <summary>
    /// Retrieves application settings using cursor-based pagination.
    /// </summary>
    /// <param name="query">Cursor query parameters for filtering and paging.</param>
    /// <param name="dataShapingService"></param>
    /// <returns>Collection of application settings with optional next cursor.</returns>
    [ProducesResponseType(typeof(CollectionResponse<ExpandoObject>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ResponseCache(Duration = 120)]
    [HttpGet("cursor")]
    public async Task<IActionResult> GetAppSettingsCursor(
        [FromQuery] AppSettingsCursorQueryParameters query,
        DataShapingService dataShapingService)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShapingService.Validate<AppSettingsDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided Fields aren't valid: '{query.Fields}'");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            query.Search = query.Search.Trim().ToLower();
        }

        IQueryable<AppSettings> appSettingsQuery = dbContext.AppSettings
            .Where(s => query.Search == null || s.Key.ToLower().Contains(query.Search))
            .Where(s => query.Type == null || s.Type == query.Type);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursor = AppSettingsCursorDto.Decode(query.Cursor);
            if (cursor is not null)
            {
                appSettingsQuery = appSettingsQuery.Where(s => string.Compare(s.Id, cursor.Id) <= 0);
            }
        }

        List<AppSettingsDto> appSettings = await appSettingsQuery
            .OrderByDescending(s => s.Id)
            .Take(query.Limit + 1)
            .Select(AppSettingsQueries.ProjectToDto())
            .ToListAsync();

        bool hasNextPage = appSettings.Count > query.Limit;
        string? nextCursor = null;

        if (hasNextPage)
        {
            AppSettingsDto lastAppSettings = appSettings[^1];
            nextCursor = AppSettingsCursorDto.Encode(lastAppSettings.Id);
            appSettings.RemoveAt(appSettings.Count - 1);
        }

        var paginationResult = new CollectionResponse<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(
                appSettings,
                query.Fields,
                query.IncludesLinks ? s => CreateLinksForAppSettings(s.Id, query.Fields) : null),
        };
        if (query.IncludesLinks)
        {
            paginationResult.Links = CreateLinksForAppSettingsCursor(query,nextCursor);
        }
        return Ok(paginationResult);
    }

    /// <summary>
    /// Retrieves a single application setting by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the application setting.</param>
    /// <param name="fields">Optional fields to shape the response.</param>
    /// <param name="accept"></param>
    /// <param name="dataShapingService"></param>
    /// <returns>The requested application setting.</returns>
    [ProducesResponseType(typeof(ExpandoObject), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 120)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppSetting(
        string id,
        string? fields,
        [FromHeader(Name="Accept")]
        string? accept,
        DataShapingService dataShapingService)
    {

        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (!dataShapingService.Validate<AppSettingsDto>(fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided Fields aren't valid: '{fields}'");
        }

        AppSettingsDto? appSetting = await dbContext
            .AppSettings
            .Select(AppSettingsQueries.ProjectToDto()).FirstOrDefaultAsync();

        if (appSetting is null)
        {
            return NotFound();
        }

        ExpandoObject ShapedAppSetting = dataShapingService.ShapeData(appSetting, fields);

        if (accept == CustomeMediaTypeNames.Application.HateoasJson)
        {
            List<LinkDto> links = CreateLinksForAppSettings(id, fields);
            ShapedAppSetting.TryAdd("links", links);
        }

        return Ok(ShapedAppSetting);
    }

    /// <summary>
    /// Creates a new application setting.
    /// </summary>
    /// <param name="createAppSettingsDto">The application setting to create.</param>
    /// <param name="validator"></param>
    /// <returns>The created application setting.</returns>
    [ProducesResponseType(typeof(AppSettingsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [IdempotentRequest]
    [HttpPost]
    public async Task<ActionResult<AppSettingsDto>> CreateAppSettings(
        CreateAppSettingsDto createAppSettingsDto,
        IValidator<CreateAppSettingsDto> validator)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createAppSettingsDto);

        AppSettings appSetting = createAppSettingsDto.ToEntity();

        if (await dbContext.AppSettings.AnyAsync(s => s.Key == appSetting.Key))
        {
            return Problem(detail: $"The Setting '{appSetting.Key}' already exists",
                           statusCode: StatusCodes.Status409Conflict);
        }

        dbContext.AppSettings.Add(appSetting);

        await dbContext.SaveChangesAsync();

        AppSettingsDto appSettingsDto = appSetting.ToDto();

        appSettingsDto.Links = CreateLinksForAppSettings(appSetting.Id, null);

        return CreatedAtAction(nameof(GetAppSettings), new { id = appSettingsDto.Id }, appSettingsDto);
    }

    /// <summary>
    /// Updates an existing application setting.
    /// </summary>
    /// <param name="id">The unique identifier of the application setting.</param>
    /// <param name="updateAppSettingsDto">The updated application setting data.</param>
    /// <returns>No content if update is successful.</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAppSettings(string id, UpdateAppSettingsDto updateAppSettingsDto)
    {

        AppSettings? AppSettings = await dbContext
            .AppSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (AppSettings is null)
        {
            return NotFound();
        }

        AppSettings.UpdateFromDto(updateAppSettingsDto);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Partially updates an existing application setting.
    /// </summary>
    /// <param name="id">The unique identifier of the application setting.</param>
    /// <param name="patchDocument">The JSON Patch document with update operations.</param>
    /// <returns>No content if update is successful.</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchAppSettings(string id, JsonPatchDocument<AppSettingsDto> patchDocument)
    {

        AppSettings? AppSettings = await dbContext
            .AppSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (AppSettings is null)
        {
            return NotFound();
        }

        AppSettingsDto AppSettingsDto = AppSettings.ToDto();

        patchDocument.ApplyTo(AppSettingsDto, ModelState);

        if (!TryValidateModel(AppSettingsDto))
        {
            return ValidationProblem(ModelState);
        }

        AppSettings.Value = AppSettingsDto.Value;
        AppSettings.ModifiedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Deletes an application setting by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the application setting.</param>
    /// <returns>No content if deletion is successful.</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAppSettings(string id)
    {

        AppSettings? AppSettings = await dbContext
            .AppSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (AppSettings is null)
        {
            return NotFound();
        }

        dbContext.AppSettings.Remove(AppSettings);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private List<LinkDto> CreateLinksForAppSettings(
        AppSettingsQueryParameters parameters,
        bool hasNextPage,
        bool hasPreviousPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetAppSettings), "self" , HttpMethods.Get , new
            {
                page     = parameters.Page,
                pageSize = parameters.PageSize,
                fields   = parameters.Fields,
                q        = parameters.Search,
                sort     = parameters.Sort,
                type     = parameters.Type
            }),
            linkService.Create(nameof(CreateAppSettings), "create" , HttpMethods.Post)
        ];

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetAppSettings), "next-page", HttpMethods.Get, new
            {
                page = parameters.Page + 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type
            }));
        }

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetAppSettings), "prev-page", HttpMethods.Get, new
            {
                page = parameters.Page - 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type
            }));
        }

        return links;
    }
    private List<LinkDto> CreateLinksForAppSettingsCursor(
        AppSettingsCursorQueryParameters parameters,
        string? nextCursor)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetAppSettingsCursor), "self" , HttpMethods.Get , new
            {
                cursor = parameters.Cursor,
                limit = parameters.Limit,
                fields   = parameters.Fields,
                q        = parameters.Search,
                type     = parameters.Type
            }),
            linkService.Create(nameof(CreateAppSettings), "create" , HttpMethods.Post)
        ];

        if (!string.IsNullOrWhiteSpace(nextCursor))
        {
            links.Add(linkService.Create(nameof(GetAppSettingsCursor), "next-page", HttpMethods.Get, new
            {
                cursor = nextCursor,
                limit = parameters.Limit,
                fields = parameters.Fields,
                q = parameters.Search,
                type = parameters.Type
            }));
        }

        return links;
    }
    private List<LinkDto> CreateLinksForAppSettings(string id, string? fields)
    {
        User.IsInRole(Roles.Admin);

        List<LinkDto> links =
        [
            linkService.Create(nameof(GetAppSettings), "self" , HttpMethods.Get , new {id , fields} ),
            linkService.Create(nameof(UpdateAppSettings), "update" , HttpMethods.Put , new {id} ),
            linkService.Create(nameof(PatchAppSettings), "partial-update" , HttpMethods.Patch , new {id} ),
            linkService.Create(nameof(DeleteAppSettings), "delete" , HttpMethods.Delete , new {id} ),
        ];
        return links;
    }

}
