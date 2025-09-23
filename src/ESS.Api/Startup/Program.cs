using ESS.Api.Database.DatabaseContext;
using ESS.Api.Middleware.Caching;
using ESS.Api.Options;
using ESS.Api.Startup;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddApiServices()
       .AddErrorHandling()
       .AddDatabase()
       .AddObservability()
       .AddApplicationServices()
       .AddAuthenticationServices()
       .AddCorsPolicy()
       .AddRateLimiting()
       .AddMinioService()
       .AddSmsService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/swagger/v1/swagger.json");
    });

    await app.ApplyMigrationsAsync();

    await app.SeedInitialDataAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseMiddleware<ETagMiddleware>();

app.MapControllers();

await app.RunAsync();


public partial class Program;
