namespace ESS.Api.DTOs.Common;

public readonly record struct LinkDto(
    string Href,
    string Rel,
    string Method);
