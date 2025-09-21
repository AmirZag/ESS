namespace ESS.Api.Database.Entities.Settings;

/// <summary>
/// Represents the different types of application settings.
/// </summary>
public enum AppSettingsType
{
    /// <summary>
    /// عمومی (General settings for the application).
    /// </summary>
    General = 0,

    /// <summary>
    /// امنیتی (Security-related settings).
    /// </summary>
    Security = 1,

    /// <summary>
    /// کاربری (User-related settings).
    /// </summary>
    Users = 2,

    /// <summary>
    /// حسابداری (Accounting settings).
    /// </summary>
    Acc = 3,

    /// <summary>
    /// کارگزینی (Human resource settings).
    /// </summary>
    HumanResource = 4,

    /// <summary>
    /// حقوق و دستمزد (Payroll settings).
    /// </summary>
    Payroll = 5,
}
