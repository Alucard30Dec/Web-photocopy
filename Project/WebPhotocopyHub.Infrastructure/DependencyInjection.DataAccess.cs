using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.DataAccess.Configuration;
using WebPhotocopyHub.DataAccess.Controllers;
using WebPhotocopyHub.DataAccess.Routines;
using WebPhotocopyHub.Infrastructure;

namespace WebPhotocopyHub.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddWebPhotocopyHubDataAccess(
        this IServiceCollection p_objServices,
        IConfiguration p_objConfiguration)
    {
        // ChatGPT 2026-06-02: entry point DataAccess de Web khong goi truc tiep Infrastructure sau khi tach module TKS.
        p_objServices.AddSingleton<IWebPhotocopyHubConnectionStringProvider, WebPhotocopyHubConnectionStringProvider>();
        p_objServices.AddSingleton<IWebPhotocopyHubRoutineCatalog, WebPhotocopyHubRoutineCatalog>();
        p_objServices.AddScoped<IWebPhotocopyHubRoutineExecutor, PostgreSqlRoutineExecutor>();
        p_objServices.AddScoped<IBackOfficeDashboardQueryService, BackOfficeDashboardQueryService>();
        p_objServices.AddScoped<IAdminUserQueryService, AdminUserQueryService>();
        p_objServices.AddInfrastructure(p_objConfiguration);
        return p_objServices;
    }
}
