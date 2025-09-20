using ESS.Api.Database.DatabaseContext;
using ESS.Api.Middleware.Caching;
using ESS.Api.Options;
using ESS.Api.Startup;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddApiServices()
       .AddErrorHandling()
       .AddDatabase()
       .AddObservability()
       .AddApplicationServices()
       .AddAuthenticationServices()
       .AddCorsPolicy()
       .AddRateLimiting()
       .AddMinioService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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
