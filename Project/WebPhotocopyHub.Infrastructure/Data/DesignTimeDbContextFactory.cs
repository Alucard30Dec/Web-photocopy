using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WebPhotocopyHub.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("WEBPHOTOCOPYHUB_POSTGRES_CONNECTION") ??
            Environment.GetEnvironmentVariable("PHOTOCOPYHUB_POSTGRES_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Thiếu connection string cho design-time EF Core. " +
                "Hãy set env var ConnectionStrings__DefaultConnection hoặc WEBPHOTOCOPYHUB_POSTGRES_CONNECTION trước khi chạy dotnet ef.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
