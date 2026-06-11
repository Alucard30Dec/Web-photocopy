using Microsoft.Extensions.DependencyInjection;

namespace WebPhotocopyHub.Report;

public static class DependencyInjection
{
    public static IServiceCollection AddWebPhotocopyHubReports(this IServiceCollection p_objServices)
    {
        p_objServices.AddScoped<IAdminCsvReportService, AdminCsvReportService>();
        return p_objServices;
    }
}
