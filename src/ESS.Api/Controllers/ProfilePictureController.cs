using System.Net.Mime;
using Asp.Versioning;
using ESS.Api.Database.Entities.Users;
using ESS.Api.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ESS.Api.Controllers;

[Authorize(Roles =Roles.Employee)]
[ApiController]
[Route("profile/imports")]
[ApiVersion("1.0")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomeMediaTypeNames.Application.JsonV1,
    CustomeMediaTypeNames.Application.HateoasJson,
    CustomeMediaTypeNames.Application.HateoasJsonV1)]
public sealed class ProfilePictureController
{
   //Requires Implementation
}
