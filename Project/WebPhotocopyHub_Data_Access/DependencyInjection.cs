using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.DataAccess.Configuration;
using PhotoCopyHub.DataAccess.Controllers;
using PhotoCopyHub.DataAccess.Routines;
using PhotoCopyHub.Infrastructure;

namespace PhotoCopyHub.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddPhotoCopyHubDataAccess(
        this IServiceCollection p_objServices,
        IConfiguration p_objConfiguration)
    {
        // ChatGPT 2026-06-02: entry point DataAccess de Web khong goi truc tiep Infrastructure sau khi tach module TKS.
        p_objServices.AddSingleton<IPhotoCopyHubConnectionStringProvider, PhotoCopyHubConnectionStringProvider>();
        p_objServices.AddSingleton<IPhotoCopyHubRoutineCatalog, PhotoCopyHubRoutineCatalog>();
        p_objServices.AddScoped<IPhotoCopyHubRoutineExecutor, PostgreSqlRoutineExecutor>();
        p_objServices.AddScoped<IBackOfficeDashboardQueryService, BackOfficeDashboardQueryService>();
        p_objServices.AddScoped<IAdminUserQueryService, AdminUserQueryService>();
        p_objServices.AddInfrastructure(p_objConfiguration);
        return p_objServices;
    }
}
