using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using ESS.Api.DTOs.Auth;
using ESS.IntegrationTests.Infrastructure;
//statics
using Employee = ESS.IntegrationTests.Infrastructure.StaticParameters.MockUsers.ValidEmployee;
using Admin = ESS.IntegrationTests.Infrastructure.StaticParameters.MockUsers.ValidAdmin;
using AnotherEmployee = ESS.IntegrationTests.Infrastructure.StaticParameters.MockUsers.AnotherValidEmployee;
using InValidEmployee = ESS.IntegrationTests.Infrastructure.StaticParameters.MockUsers.InValidEmployee;
using Auth = ESS.IntegrationTests.Infrastructure.StaticParameters.Routes.Auth;

namespace ESS.IntegrationTests.Tests;
public sealed class AuthenticationTests(EssWebAppFactory factory) : IntegrationTestFixture(factory)
{
    [Fact]
    public async Task Register_ShouldSucceed_WithValidParameters()
    {
        //Arrange
        var dto = new RegisterUserDto 
        {
            NationalCode = Employee.NationalCode,
            Password = Employee.Password,
            PhoneNumber = Employee.PhoneNumber,
        };
        HttpClient client = CreateClient();

        //Act
       HttpResponseMessage response = await client.PostAsJsonAsync(Auth.Register, dto);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldFail_WithInValidParameters()
    {
        //Arrange
        var dto = new RegisterUserDto
        {
            NationalCode = InValidEmployee.NationalCode,
            Password = InValidEmployee.NationalCode,
            PhoneNumber = InValidEmployee.NationalCode,
        };
        HttpClient client = CreateClient();

        //Act
        HttpResponseMessage response = await client.PostAsJsonAsync(Auth.Register, dto);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnAccessTokens_WithValidParameters()
    {
        //Arrange
        var dto = new RegisterUserDto
        {
            NationalCode = AnotherEmployee.NationalCode,
            Password = AnotherEmployee.Password,
            PhoneNumber = AnotherEmployee.PhoneNumber
        };
        HttpClient client = CreateClient();

        //Act
        HttpResponseMessage response = await client.PostAsJsonAsync(Auth.Register, dto);
        response.EnsureSuccessStatusCode();

        //Assert
        AccessTokensDto? accessTokenDto = await response.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(accessTokenDto);
    }

    [Fact]
    public async Task LoginAdmin_ShouldSucceed_WithValidParameters()
    {
        //Arrange
        var dto = new LoginUserDto
        {
            NationalCode = Admin.NationalCode,
            Password = Admin.Password,
        };
        HttpClient client = CreateClient();

        //Act
        HttpResponseMessage response = await client.PostAsJsonAsync(Auth.Login, dto);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]

    public async Task LoginAdmin_ShouldReturnAccessTokens_WithValidParameters()
    {
        //Arrange
        var dto = new LoginUserDto
        {
            NationalCode = Admin.NationalCode,
            Password = Admin.Password,
        };
        HttpClient client = CreateClient();

        //Act
        HttpResponseMessage response = await client.PostAsJsonAsync(Auth.Login, dto);
        response.EnsureSuccessStatusCode();

        //Assert
        AccessTokensDto? accessTokenDto = await response.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(accessTokenDto);
    }
}
