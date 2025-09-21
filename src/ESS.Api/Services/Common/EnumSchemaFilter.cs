using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ESS.Api.Services.Common;

public sealed class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            var enumDescriptions = new List<string>();
            foreach (var field in context.Type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                var displayAttr = field.GetCustomAttributes(typeof(DisplayAttribute), false)
                    .Cast<DisplayAttribute>()
                    .FirstOrDefault();

                var name = displayAttr?.Name ?? field.Name;
                var value = (int)Enum.Parse(context.Type, field.Name);

                enumDescriptions.Add($"{value} = {name}");
            }

            var description = schema.Description ?? string.Empty;
            schema.Description = (description + " " + string.Join(", ", enumDescriptions)).Trim();
        }
    }
}
