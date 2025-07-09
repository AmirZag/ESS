namespace ESS.Api.Options;

public sealed class CorsOptions
{
    public const string PolicyName = "AmardEccCorsPolicy";
    public const string SectionName = "Cors";

    public required string[] AllowedOrigins {  get; init; }
}
