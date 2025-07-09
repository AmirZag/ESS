using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ESS.Api.Extentions;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var memberInfo = value.GetType().GetMember(value.ToString());
        if (memberInfo.Length == 0)
        {
            return value.ToString();
        }
        var attr = memberInfo[0].GetCustomAttribute<DisplayAttribute>();
        return attr?.Name ?? value.ToString();
    }
}
