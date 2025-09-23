namespace ESS.Api.Services.Common.Interfaces;

public interface ISmsService
{
    Task<(bool Success, string Message)> SendVerificationCode(string phoneNumber, string code);
}
