namespace ESS.Api.Infrastructure.Sms.Dto;

public sealed class SendSmsRequest
{
    public string Username { get; init; }
    public string Password { get; init; }
    public string To { get; set; }
    public string From { get; init; }
    public string Text { get; set; }
}
