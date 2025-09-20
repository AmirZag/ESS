using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace ESS.IntegrationTests.Infrastructure;

public sealed class EssWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("amard_ess")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DATABASECONNECTIONSTRING", _postgresSqlContainer.GetConnectionString());
    }

    public async Task InitializeAsync()
    {
        await _postgresSqlContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgresSqlContainer.StopAsync();
    }
}
