using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using ESS.Api.Database.Entities.Settings;
using ESS.Api.DTOs.Settings;
using ESS.IntegrationTests.Infrastructure;
using Admin = ESS.IntegrationTests.Infrastructure.StaticParameters.MockUsers.ValidAdmin;
using AppSettings = ESS.IntegrationTests.Infrastructure.StaticParameters.Routes.AppSettings;
using System.Net;

namespace ESS.IntegrationTests.Tests;
public sealed class SettingsTests(EssWebAppFactory factory) : IntegrationTestFixture(factory)
{

    [Fact]
    public async Task CreateSettings_ShouldSucceed_WithValidParameters()
    {
        //Arrange
        var dto = new CreateAppSettingsDto
        {
            Key = AppSettingsKey.CheckWeakPassword,
            Type = AppSettingsType.Security,
            Value = "1",
            Description = "Just for Testing Purposes"
        };

        var client = await CreateAuthenticatedClientAsync(Admin.NationalCode, Admin.Password);

        //Act
        HttpResponseMessage response = await client.PostAsJsonAsync(AppSettings.Settings,dto);

        //Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<AppSettingsDto>());

    }

}
