namespace ESS.Api.Options;

public sealed class MelliPayamakOptions
{
    public string Username { get; init; }
    public string Password { get; init; }
    public string FromNumber { get; init; }
    public string ApiUrl { get; init; }
    public string VerificationTemplate { get; init; }
}
