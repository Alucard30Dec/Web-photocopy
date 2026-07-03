using Microsoft.Extensions.Configuration;
using WebPhotocopyHub.Infrastructure;

namespace WebPhotocopyHub.DataAccess.Configuration;

public interface IWebPhotocopyHubConnectionStringProvider
{
    string Get_Connection_String();
}

public sealed class WebPhotocopyHubConnectionStringProvider : IWebPhotocopyHubConnectionStringProvider
{
    private readonly IConfiguration _configuration;

    public WebPhotocopyHubConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Get_Connection_String()
    {
        return global::WebPhotocopyHub.Infrastructure.DependencyInjection.ResolveLocalPostgreSqlConnectionString(_configuration);
    }
}
