using System.Data.Common;
using Microsoft.Extensions.Configuration;

namespace PhotoCopyHub.DataAccess.Configuration;

public interface IPhotoCopyHubConnectionStringProvider
{
    string Get_Connection_String();
}

public sealed class PhotoCopyHubConnectionStringProvider : IPhotoCopyHubConnectionStringProvider
{
    private const string c_strDefaultApplicationName = "DTBWebPhotocopyHub";
    private readonly IConfiguration _configuration;

    public PhotoCopyHubConnectionStringProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Get_Connection_String()
    {
        var v_arrCandidate = new[]
        {
            _configuration.GetConnectionString("DefaultConnection"),
            _configuration.GetConnectionString("PostgreSqlConnection"),
            _configuration["PHOTOCOPYHUB_POSTGRES_CONNECTION"],
            Environment.GetEnvironmentVariable("PHOTOCOPYHUB_POSTGRES_CONNECTION"),
            Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSqlConnection"),
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        };

        var v_arrValidationMessage = new List<string>();

        foreach (var v_strCandidate in v_arrCandidate)
        {
            try
            {
                var v_strNormalized = Normalize_PostgreSql_Connection_String(v_strCandidate);
                if (!string.IsNullOrWhiteSpace(v_strNormalized))
                {
                    return v_strNormalized;
                }
            }
            catch (InvalidOperationException v_objException)
            {
                v_arrValidationMessage.Add(v_objException.Message);
            }
            catch (ArgumentException v_objException)
            {
                v_arrValidationMessage.Add(v_objException.Message);
            }
        }

        var v_strDetail = v_arrValidationMessage.Count > 0
            ? " Chi tiết lỗi đã gặp: " + string.Join(" | ", v_arrValidationMessage.Distinct())
            : string.Empty;

        throw new InvalidOperationException(
            "PostgreSQL/Supabase connection string chưa được cấu hình đúng. " +
            "Hãy set ConnectionStrings:DefaultConnection hoặc PHOTOCOPYHUB_POSTGRES_CONNECTION. " +
            "Với Supabase hosted, nên dùng Session Pooler URI dạng " +
            "postgres://postgres.<project-ref>:[YOUR-PASSWORD]@aws-...pooler.supabase.com:5432/postgres. " +
            "Database trong Supabase hosted phải là postgres; DTBWebPhotocopyHub chỉ dùng làm Application Name." +
            v_strDetail);
    }

    private static string? Normalize_PostgreSql_Connection_String(string? p_strConnectionString)
    {
        if (string.IsNullOrWhiteSpace(p_strConnectionString))
        {
            return null;
        }

        var v_strTrimmed = p_strConnectionString.Trim();

        if (Looks_Like_PostgreSql_Uri(v_strTrimmed))
        {
            return Convert_PostgreSql_Uri_To_Npgsql_Connection_String(v_strTrimmed);
        }

        if (Looks_Like_PostgreSql_Connection_String(v_strTrimmed))
        {
            return Ensure_Provider_Defaults(v_strTrimmed);
        }

        return null;
    }

    private static bool Looks_Like_PostgreSql_Uri(string p_strConnectionString)
    {
        return Uri.TryCreate(p_strConnectionString, UriKind.Absolute, out var v_objUri)
            && (v_objUri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase)
                || v_objUri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase));
    }

    private static string Convert_PostgreSql_Uri_To_Npgsql_Connection_String(string p_strConnectionString)
    {
        if (!Uri.TryCreate(p_strConnectionString, UriKind.Absolute, out var v_objUri))
        {
            throw new InvalidOperationException("PostgreSQL URI không hợp lệ. Hãy kiểm tra lại connection string Supabase.");
        }

        if (!v_objUri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase)
            && !v_objUri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("PostgreSQL URI phải bắt đầu bằng postgres:// hoặc postgresql://.");
        }

        if (string.IsNullOrWhiteSpace(v_objUri.Host))
        {
            throw new InvalidOperationException("PostgreSQL URI thiếu host.");
        }

        var v_arrUserInfoPart = v_objUri.UserInfo.Split(':', 2);
        if (v_arrUserInfoPart.Length == 0 || string.IsNullOrWhiteSpace(v_arrUserInfoPart[0]))
        {
            throw new InvalidOperationException("PostgreSQL URI thiếu username.");
        }

        var v_strDatabase = v_objUri.AbsolutePath.Trim('/');
        if (string.IsNullOrWhiteSpace(v_strDatabase))
        {
            v_strDatabase = "postgres";
        }

        var v_strUsername = Uri.UnescapeDataString(v_arrUserInfoPart[0]);
        var v_strPassword = v_arrUserInfoPart.Length > 1
            ? Uri.UnescapeDataString(v_arrUserInfoPart[1])
            : string.Empty;

        var v_objBuilder = new DbConnectionStringBuilder
        {
            ["Host"] = v_objUri.IdnHost,
            ["Port"] = v_objUri.Port > 0 ? v_objUri.Port : 5432,
            ["Database"] = Uri.UnescapeDataString(v_strDatabase),
            ["Username"] = v_strUsername,
            ["SSL Mode"] = "Require",
            ["Trust Server Certificate"] = true,
            ["Application Name"] = c_strDefaultApplicationName
        };

        if (!string.IsNullOrEmpty(v_strPassword))
        {
            v_objBuilder["Password"] = v_strPassword;
        }

        return Ensure_Provider_Defaults(v_objBuilder.ConnectionString);
    }

    private static string Ensure_Provider_Defaults(string p_strConnectionString)
    {
        var v_objBuilder = new DbConnectionStringBuilder();

        try
        {
            v_objBuilder.ConnectionString = p_strConnectionString;
        }
        catch (ArgumentException v_objException)
        {
            throw new InvalidOperationException("Connection string PostgreSQL không parse được. Hãy copy lại connection string từ Supabase Connect.", v_objException);
        }

        Normalize_Alias(v_objBuilder, "Server", "Host");
        Normalize_Alias(v_objBuilder, "User ID", "Username");
        Normalize_Alias(v_objBuilder, "User Id", "Username");
        Normalize_Alias(v_objBuilder, "UserID", "Username");
        Normalize_Alias(v_objBuilder, "User", "Username");
        Normalize_Alias(v_objBuilder, "Pwd", "Password");

        var v_strHost = Get_Required_Value(v_objBuilder, "Host", "Host");
        var v_strUsername = Get_Required_Value(v_objBuilder, "Username", "Username");
        var v_strDatabase = Try_Get_Any_String(v_objBuilder, out var v_strDatabaseValue, "Database")
            ? v_strDatabaseValue
            : "postgres";

        v_objBuilder["Host"] = v_strHost;
        v_objBuilder["Username"] = v_strUsername;
        v_objBuilder["Database"] = v_strDatabase;

        if (!Try_Get_Any_String(v_objBuilder, out _, "Port"))
        {
            v_objBuilder["Port"] = 5432;
        }

        if (Is_Supabase_Host(v_strHost))
        {
            if (!v_strDatabase.Equals("postgres", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Connection string Supabase đang dùng Database=" + v_strDatabase + ". " +
                    "Với Supabase hosted, hãy dùng Database=postgres. " +
                    "Tên DTBWebPhotocopyHub nên dùng làm project name hoặc Application Name, không dùng làm database name.");
            }

            var v_strPassword = Get_Required_Value(v_objBuilder, "Password", "Password");
            if (v_strPassword.Contains("[YOUR-PASSWORD]", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Connection string Supabase vẫn còn placeholder [YOUR-PASSWORD]. Hãy thay bằng database password thật.");
            }

            if (Is_Supabase_Direct_Host(v_strHost) && !Is_Direct_Supabase_Host_Allowed())
            {
                throw new InvalidOperationException(
                    "Bạn đang dùng Supabase Direct host '" + v_strHost + "'. Direct host dạng db.<project-ref>.supabase.co thường cần IPv6 hoặc Supabase IPv4 add-on. " +
                    "Để chạy ổn trên Windows/local IPv4, hãy copy Session Pooler URI từ Supabase Dashboard > Connect > Session pooler, host dạng aws-<region>.pooler.supabase.com, port 5432. " +
                    "Nếu bạn chắc chắn đã bật IPv4 add-on hoặc có IPv6, set PHOTOCOPYHUB_ALLOW_SUPABASE_DIRECT=true để cho phép Direct host.");
            }

            v_objBuilder["SSL Mode"] = "Require";

            if (!Try_Get_Any_String(v_objBuilder, out _, "Trust Server Certificate"))
            {
                v_objBuilder["Trust Server Certificate"] = true;
            }

            if (!Try_Get_Any_String(v_objBuilder, out _, "Application Name"))
            {
                v_objBuilder["Application Name"] = c_strDefaultApplicationName;
            }
        }

        return v_objBuilder.ConnectionString;
    }

    private static bool Looks_Like_PostgreSql_Connection_String(string p_strConnectionString)
    {
        if (p_strConnectionString.Contains(".db", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (p_strConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (p_strConnectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return p_strConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || p_strConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || p_strConnectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase)
            || p_strConnectionString.Contains("User ID=", StringComparison.OrdinalIgnoreCase);
    }

    private static void Normalize_Alias(DbConnectionStringBuilder p_objBuilder, string p_strSourceKey, string p_strTargetKey)
    {
        if (!p_objBuilder.ContainsKey(p_strTargetKey) && p_objBuilder.TryGetValue(p_strSourceKey, out var v_objRawValue) && v_objRawValue is not null)
        {
            p_objBuilder[p_strTargetKey] = v_objRawValue;
            p_objBuilder.Remove(p_strSourceKey);
        }
    }

    private static string Get_Required_Value(DbConnectionStringBuilder p_objBuilder, string p_strDisplayName, params string[] p_arrKey)
    {
        if (Try_Get_Any_String(p_objBuilder, out var v_strValue, p_arrKey))
        {
            return v_strValue;
        }

        throw new InvalidOperationException("Connection string PostgreSQL thiếu hoặc rỗng: " + p_strDisplayName + ".");
    }

    private static bool Try_Get_Any_String(DbConnectionStringBuilder p_objBuilder, out string p_strValue, params string[] p_arrKey)
    {
        foreach (var v_strKey in p_arrKey)
        {
            if (p_objBuilder.TryGetValue(v_strKey, out var v_objRawValue) && v_objRawValue is not null)
            {
                p_strValue = v_objRawValue.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(p_strValue))
                {
                    return true;
                }
            }
        }

        p_strValue = string.Empty;
        return false;
    }

    private static bool Is_Supabase_Host(string p_strHost)
    {
        return p_strHost.Contains("supabase.co", StringComparison.OrdinalIgnoreCase)
            || p_strHost.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Is_Supabase_Direct_Host(string p_strHost)
    {
        return p_strHost.StartsWith("db.", StringComparison.OrdinalIgnoreCase)
            && p_strHost.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Is_Direct_Supabase_Host_Allowed()
    {
        var v_strValue = Environment.GetEnvironmentVariable("PHOTOCOPYHUB_ALLOW_SUPABASE_DIRECT");
        return string.Equals(v_strValue, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v_strValue, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v_strValue, "yes", StringComparison.OrdinalIgnoreCase);
    }
}