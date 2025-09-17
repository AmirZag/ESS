using ESS.Api.DTOs.Common;

namespace ESS.Api.Services.Common;

public sealed class LinkService(LinkGenerator linkGenerator , IHttpContextAccessor httpContextAccessor)
{
    public LinkDto Create(
        string endpointName,
        string rel,
        string method,
        object? values = null,
        string? controller = null)
    {
        string? href = linkGenerator.GetUriByAction(
            httpContextAccessor.HttpContext!,
            endpointName,
            controller,
            values
            );

        return new LinkDto(
            href ?? throw new Exception("Invalid endpoint name provided."),
            rel,
            method
        );

    }
}
