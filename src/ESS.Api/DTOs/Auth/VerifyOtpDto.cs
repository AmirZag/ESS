namespace ESS.Api.DTOs.Auth;

public sealed class VerifyOtpDto
{
    public string PhoneNumber { get; set; }
    public string Code { get; set; }
}
