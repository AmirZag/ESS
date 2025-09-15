using System.Net.Mime;
using Asp.Versioning;
using ESS.Api.Database.Entities.Users;
using ESS.Api.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ESS.Api.Controllers.Users;

[Authorize(Roles =Roles.Employee)]
[ApiController]
[EnableRateLimiting("default")]
[Route("users/avatar")]
[ApiVersion("1.0")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomeMediaTypeNames.Application.JsonV1,
    CustomeMediaTypeNames.Application.HateoasJson,
    CustomeMediaTypeNames.Application.HateoasJsonV1)]
public sealed class AvatarController
{
   //Requires Implementation
}
