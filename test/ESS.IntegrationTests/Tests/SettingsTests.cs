using System.Net;
using System.Net.Http.Json;
using ESS.Api.Database.Entities.Settings;
using ESS.Api.DTOs.Settings;
using ESS.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Admin = ESS.IntegrationTests.Infrastructure.StaticParameters.MockUsers.ValidAdmin;
using AppSettings = ESS.IntegrationTests.Infrastructure.StaticParameters.Routes.AppSettings;

namespace ESS.IntegrationTests.Tests;
public sealed class SettingsTests(EssWebAppFactory factory) : IntegrationTestFixture(factory)
{

    [Fact]
    public async Task CreateSettings_ShouldSucceed_WithIdempotencyKey()
    {
        var dto = new CreateAppSettingsDto
        {
            Key = AppSettingsKey.CheckWeakPassword,
            Type = AppSettingsType.Security,
            Value = "1",
            Description = "Just for Testing Purposes"
        };

        var client = await CreateAuthenticatedClientAsync(Admin.NationalCode, Admin.Password);

        using var request = new HttpRequestMessage(HttpMethod.Post, AppSettings.Settings)
        {
            Content = JsonContent.Create(dto)
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.amard-ecc.hateoas+json"));

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<AppSettingsDto>());
    }

    [Fact]
    public async Task CreateSettings_ShouldFail_WithInValidParameters()
    {
        var dto = new CreateAppSettingsDto
        {
            Key = "Settings",
            Type = AppSettingsType.Security,
            Value = "1",
            Description = "Just for Testing Purposes"
        };

        var client = await CreateAuthenticatedClientAsync(Admin.NationalCode, Admin.Password);

        //Act
        HttpResponseMessage response = await client.PostAsJsonAsync(AppSettings.Settings, dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.Null(locationHeader);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);

    }
}
