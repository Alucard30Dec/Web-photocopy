using Microsoft.Extensions.DependencyInjection;

namespace PhotoCopyHub.Report;

public static class DependencyInjection
{
    public static IServiceCollection AddPhotoCopyHubReports(this IServiceCollection p_objServices)
    {
        p_objServices.AddScoped<IAdminCsvReportService, AdminCsvReportService>();
        return p_objServices;
    }
}
