namespace ESS.Api.Infrastructure.Sms.Dto;

public sealed class SendSmsResponse
{
    public string Value { get; set; }
    public int RetStatus { get; set; }
    public string StrRetStatus { get; set; }
}
