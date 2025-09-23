
using System.Net.Http;
using ESS.Api.Infrastructure.Sms.Dto;
using ESS.Api.Services.Common.Interfaces;
using Microsoft.Extensions.Options;
using static System.Net.Mime.MediaTypeNames;

namespace ESS.Api.Infrastructure.Sms;

public sealed class MelliPayamakSmsService(
    IHttpClientFactory httpClientFactory,
    IOptions<MelliPayamakOptions> options,
    ILogger<MelliPayamakSmsService> logger
    ) : ISmsService
{

    private static readonly Dictionary<int, string> ErrorMessages = new()
    {
        { 0, "نام کاربری یا رمز عبور اشتباه می باشد" },
        { 2, "اعتبار کافی نمی باشد" },
        { 3, "محدودیت در ارسال روزانه" },
        { 4, "محدودیت در حجم ارسال" },
        { 5, "شماره فرستنده معتبر نمی باشد" },
        { 6, "سامانه در حال بروزرسانی می باشد" },
        { 7, "متن حاوی کلمه فیلتر شده می باشد" },
        { 9, "ارسال از خطوط عمومی از طریق وب سرویس امکان پذیر نمی باشد" },
        { 10, "کاربر مورد نظر فعال نمی باشد" },
        { 11, "ارسال نشده" },
        { 12, "مدارک کاربر کامل نمی باشد" },
        { 14, "متن حاوی لینک می باشد" },
        { 15, "عدم وجود لغو 11 در انتهای متن پیامک" },
        { 16, "شماره گیرنده ای یافت نشد" },
        { 17, "متن پیامک خالی می باشد" },
        { 18, "شماره موبایل معتبر نمی باشد" }
    };

    public async Task<(bool Success, string Message)> SendVerificationCode(string phoneNumber, string code)
    {
        string text = options.Value.VerificationTemplate
                                .Replace("{code}", code)
                                .Replace("\\n", Environment.NewLine)
                                .Replace("\\", string.Empty);


        var request = new SendSmsRequest
        {
            Username = options.Value.Username,
            Password = options.Value.Password,
            To = phoneNumber,
            From = options.Value.FromNumber,
            Text = text
        };

        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(options.Value.ApiUrl, request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("SMS API returned status code: {StatusCode}", response.StatusCode);
                return (false, "خطا در ارسال پیامک");
            }

            var result = await response.Content.ReadFromJsonAsync<SendSmsResponse>();
            if (result is null)
            {
                return (false, "خطا در دریافت پاسخ از سرویس پیامک");
            }

            if (result.RetStatus == 1)
            {
                logger.LogInformation("SMS sent successfully. RecId: {RecId}", result.Value);
                return (true, result.Value);
            }

            var errorCode = int.TryParse(result.Value, out var parsed) ? parsed : result.RetStatus;
            var errorMessage = ErrorMessages.GetValueOrDefault(errorCode, "خطای نامشخص");

            logger.LogWarning("SMS send failed. Error: {Error}", errorMessage);

            return (false, errorMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while sending SMS");
            return (false, "خطا در ارتباط با سرویس پیامک");
        }
    }
}
