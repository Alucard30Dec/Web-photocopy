using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Infrastructure.Data;
using WebPhotocopyHub.Infrastructure.Options;
using WebPhotocopyHub.Infrastructure.Services;

namespace WebPhotocopyHub.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultApplicationName = "WebPhotocopyHub";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<BusinessOptions>(configuration.GetSection(BusinessOptions.SectionName));
        services.Configure<OfficePreviewOptions>(configuration.GetSection(OfficePreviewOptions.SectionName));

        var connectionString = ResolveLocalPostgreSqlConnectionString(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
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

        services.AddScoped<IBranchContext, BranchContext>();
        services.AddScoped<IBranchManagementService, BranchManagementService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ITopUpService, TopUpService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IOfficePreviewService, OfficePreviewService>();
        services.AddScoped<IPrintJobService, PrintJobService>();
        services.AddScoped<IProductOrderService, ProductOrderService>();
        services.AddScoped<ISupportServiceOrderService, SupportServiceOrderService>();
        services.AddScoped<IPricingRuleService, PricingRuleService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IWalletReconciliationService, WalletReconciliationService>();
        services.AddScoped<ISystemAdministrationService, SystemAdministrationService>();
        services.AddScoped<IDbInitializer, DbInitializer>();
        services.AddScoped<IEmailSender, DummyEmailSender>();

        return services;
    }

    public static string ResolveLocalPostgreSqlConnectionString(IConfiguration configuration)
    {
        var candidates = new[]
        {
            configuration.GetConnectionString("DefaultConnection"),
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        };

        var validationMessages = new List<string>();

        foreach (var candidate in candidates)
        {
            try
            {
                var normalized = NormalizeLocalPostgreSqlConnectionString(candidate);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    return normalized;
                }
            }
            catch (InvalidOperationException ex)
            {
                validationMessages.Add(ex.Message);
            }
            catch (ArgumentException ex)
            {
                validationMessages.Add(ex.Message);
            }
        }

        var detail = validationMessages.Count > 0
            ? " Chi tiết lỗi đã gặp: " + string.Join(" | ", validationMessages.Distinct())
            : string.Empty;

        throw new InvalidOperationException(
            "PostgreSQL local connection string chưa được cấu hình đúng. " +
            "Hãy set ConnectionStrings:DefaultConnection hoặc biến môi trường ConnectionStrings__DefaultConnection." +
            detail);
    }

    public static string? NormalizeLocalPostgreSqlConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var trimmed = connectionString.Trim();
        var builder = LooksLikePostgreSqlUri(trimmed)
            ? ConvertPostgreSqlUriToBuilder(trimmed)
            : new NpgsqlConnectionStringBuilder(trimmed);

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            throw new InvalidOperationException("Connection string PostgreSQL thiếu Host.");
        }

        if (!IsLocalHost(builder.Host))
        {
            throw new InvalidOperationException("Ứng dụng chỉ được cấu hình PostgreSQL local qua localhost, 127.0.0.1 hoặc ::1.");
        }

        if (builder.Port <= 0)
        {
            builder.Port = 5432;
        }

        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            throw new InvalidOperationException("Connection string PostgreSQL thiếu Database.");
        }

        if (string.IsNullOrWhiteSpace(builder.Username))
        {
            throw new InvalidOperationException("Connection string PostgreSQL thiếu Username.");
        }

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            builder.ApplicationName = DefaultApplicationName;
        }

        return builder.ConnectionString;
    }

    private static bool LooksLikePostgreSqlUri(string connectionString)
    {
        return Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase));
    }

    private static NpgsqlConnectionStringBuilder ConvertPostgreSqlUriToBuilder(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("PostgreSQL URI không hợp lệ.");
        }

        var userInfoParts = uri.UserInfo.Split(':', 2);
        if (userInfoParts.Length == 0 || string.IsNullOrWhiteSpace(userInfoParts[0]))
        {
            throw new InvalidOperationException("PostgreSQL URI thiếu username.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.IdnHost,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
            Username = Uri.UnescapeDataString(userInfoParts[0]),
            ApplicationName = DefaultApplicationName
        };

        if (userInfoParts.Length > 1)
        {
            builder.Password = Uri.UnescapeDataString(userInfoParts[1]);
        }

        return builder;
    }

    private static bool IsLocalHost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}
