using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WebPhotocopyHub.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string WebProjectFolderName = "WebPhotocopyHub.Web";
    private const string WebProjectFileName = "WebPhotocopyHub.Web.csproj";
    private const string WebProjectUserSecretsId = "ef5b2168-7125-48e0-9a39-31c37cdda9b2";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = Infrastructure.DependencyInjection.ResolveLocalPostgreSqlConnectionString(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var webProjectPath = FindWebProjectPath()
            ?? Directory.GetCurrentDirectory();

        return new ConfigurationBuilder()
            .SetBasePath(webProjectPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddJsonFile(GetUserSecretsPath(), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string GetUserSecretsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "UserSecrets",
            WebProjectUserSecretsId,
            "secrets.json");
    }

    private static string? FindWebProjectPath()
    {
        foreach (var startPath in GetSearchStartPaths())
        {
            var current = new DirectoryInfo(startPath);
            while (current is not null)
            {
                var directProjectFile = Path.Combine(current.FullName, WebProjectFileName);
                if (File.Exists(directProjectFile))
                {
                    return current.FullName;
                }

                var childProjectFile = Path.Combine(current.FullName, WebProjectFolderName, WebProjectFileName);
                if (File.Exists(childProjectFile))
                {
                    return Path.Combine(current.FullName, WebProjectFolderName);
                }

                current = current.Parent;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetSearchStartPaths()
    {
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
    }
}
