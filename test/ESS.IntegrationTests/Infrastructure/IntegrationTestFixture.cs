using System.Net.Http.Headers;
using System.Net.Http.Json;
using ESS.Api.Database.DatabaseContext.ApplicationDbContexts;
using ESS.Api.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ValidEmployee = ESS.IntegrationTests.Infrastructure.StaticParameters.MockUsers.ValidEmployee;

namespace ESS.IntegrationTests.Infrastructure;
public abstract class IntegrationTestFixture(EssWebAppFactory factory) : IClassFixture<EssWebAppFactory>
{
    private HttpClient? _authorizedClient;
    public HttpClient CreateClient() => factory.CreateClient();

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string nationalCode = ValidEmployee.NationalCode,
        string password = ValidEmployee.Password)
    {
        if (_authorizedClient is not null)
        {
            return _authorizedClient;
        }

        HttpClient client = CreateClient();

        bool userExists;
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userExists = await dbContext.Users.AnyAsync(u => u.NationalCode == nationalCode);
        }

        if (!userExists)
        {
            HttpResponseMessage registerResponse = await client.PostAsJsonAsync(StaticParameters.Routes.Auth.Register,
            new RegisterUserDto
            {
                NationalCode = ValidEmployee.NationalCode,
                Password = ValidEmployee.Password,
                PhoneNumber = ValidEmployee.PhoneNumber
            });

            registerResponse.EnsureSuccessStatusCode();
        }
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(StaticParameters.Routes.Auth.Login,
            new LoginUserDto
            {
                NationalCode = nationalCode,
                Password = password
            });
        loginResponse.EnsureSuccessStatusCode();

        AccessTokensDto? loginResult = await loginResponse.Content.ReadFromJsonAsync<AccessTokensDto>();

        if (loginResult?.AccessToken is null)
        {
            throw new InvalidOperationException("Failed to get Authentication Token");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        _authorizedClient = client;

        return client;

    }
}
