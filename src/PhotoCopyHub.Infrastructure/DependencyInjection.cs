using System.Data.Common;
using System.Net.Sockets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Infrastructure.Data;
using PhotoCopyHub.Infrastructure.Options;
using PhotoCopyHub.Infrastructure.Services;

namespace PhotoCopyHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=photocopyhub.db";
        var fallbackSqliteConnectionString = configuration.GetConnectionString("FallbackSqliteConnection")
                                            ?? "Data Source=photocopyhub.fallback.db";
        var databaseProvider = configuration["DatabaseProvider"] ?? "Sqlite";
        var useSqliteFallback = configuration.GetValue<bool?>("Database:UseSqliteFallbackWhenPrimaryUnavailable") ?? true;
        var primaryConnectTimeoutMs = configuration.GetValue<int?>("Database:PrimaryConnectTimeoutMs") ?? 3000;

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else if (databaseProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase) ||
                     databaseProvider.Equals("TiDb", StringComparison.OrdinalIgnoreCase))
            {
                if (useSqliteFallback &&
                    !CanOpenTcpToMySqlHost(connectionString, primaryConnectTimeoutMs, out var probeMessage))
                {
                    Console.WriteLine(
                        $"[PhotoCopyHub] Khong the ket noi MySQL/TiDB ({probeMessage}). " +
                        $"Tu dong fallback sang SQLite: {fallbackSqliteConnectionString}");
                    options.UseSqlite(fallbackSqliteConnectionString);
                }
                else
                {
                    options.UseMySql(
                        connectionString,
                        new MySqlServerVersion(new Version(8, 0, 0)),
                        mysqlOptions => mysqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
                }
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Stores.MaxLengthForKeys = 191;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<BusinessOptions>(configuration.GetSection(BusinessOptions.SectionName));

        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ITopUpService, TopUpService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IPrintJobService, PrintJobService>();
        services.AddScoped<IProductOrderService, ProductOrderService>();
        services.AddScoped<ISupportServiceOrderService, SupportServiceOrderService>();
        services.AddScoped<IPricingRuleService, PricingRuleService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IWalletReconciliationService, WalletReconciliationService>();
        services.AddScoped<IDbInitializer, DbInitializer>();

        return services;
    }

    private static bool CanOpenTcpToMySqlHost(string connectionString, int timeoutMs, out string message)
    {
        message = "khong xac dinh";

        if (!TryParseServerAndPort(connectionString, out var server, out var port, out var parseError))
        {
            message = parseError;
            return false;
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(server, port);
            var finished = Task.WhenAny(connectTask, Task.Delay(timeoutMs)).GetAwaiter().GetResult();
            if (finished != connectTask)
            {
                message = $"timeout {timeoutMs}ms toi {server}:{port}";
                return false;
            }

            // Ensure any socket-level exception is observed.
            connectTask.GetAwaiter().GetResult();
            message = $"ket noi OK toi {server}:{port}";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    private static bool TryParseServerAndPort(
        string connectionString,
        out string server,
        out int port,
        out string error)
    {
        server = string.Empty;
        port = 3306;
        error = string.Empty;

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (TryRead(builder, out server, "Server", "Host", "Data Source"))
            {
                if (TryRead(builder, out var portRaw, "Port") &&
                    int.TryParse(portRaw, out var parsedPort) &&
                    parsedPort > 0)
                {
                    port = parsedPort;
                }

                return true;
            }

            error = "khong tim thay Server/Host trong connection string";
            return false;
        }
        catch (Exception ex)
        {
            error = $"connection string khong hop le: {ex.Message}";
            return false;
        }
    }

    private static bool TryRead(DbConnectionStringBuilder builder, out string value, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var obj) &&
                obj is not null &&
                !string.IsNullOrWhiteSpace(obj.ToString()))
            {
                value = obj.ToString()!;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
