using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using ESS.Api.Services.Common;

namespace ESS.Api.DTOs.Common;

public record QueryParameter : AcceptHeaderDto
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public string? Sort { get; init; }
    public string? Fields { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
