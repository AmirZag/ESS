namespace ESS.Api.Services.Sms;

public interface ISmsService
{
    Task<(bool Success, string Message)> SendVerificationCode(string phoneNumber, string code);
}
