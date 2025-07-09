using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ESS.Api.Database.Entities.Settings;

public enum AppSettingsType
{
    [Display(Name = "عمومی")]
    General = 0,

    [Display(Name = "امنیتی")]
    Security = 1,

    [Display(Name = "کاربری")]
    Users = 2,

    [Display(Name = "حسابداری")]
    Acc = 3,

    [Display(Name = "کارگزینی")]
    HumanResource = 4,

    [Display(Name = "حقوق و دستمزد")]
    Payroll = 5,
}
