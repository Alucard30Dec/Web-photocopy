using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;

namespace WebPhotocopyHub.Infrastructure.Data;

public class DbInitializer : IDbInitializer
{
    private static readonly byte[] SeedPdfContent = Encoding.UTF8.GetBytes(
        "%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 300] >>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF");

    private readonly ApplicationDbContext _dbContext;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DbInitializer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public DbInitializer(
        ApplicationDbContext dbContext,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<DbInitializer> logger,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseReadyAsync(cancellationToken);

        await SeedRolesAsync();
        await SeedAdminAsync();
        await SeedShopOperatorAsync();
        await SeedDefaultCustomerAccountsAsync();
        await SeedPricingAsync(cancellationToken);
        await SeedProductsAsync(cancellationToken);
        await SeedSupportServicesAsync(cancellationToken);
        try
        {
            await SeedSampleDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Seed dữ liệu mẫu thất bại. Ứng dụng vẫn tiếp tục chạy.");
            if (IsSeedOnlyMode())
            {
                throw;
            }
        }
    }

    private async Task EnsureDatabaseReadyAsync(CancellationToken cancellationToken)
    {
        var providerName = _dbContext.Database.ProviderName ?? string.Empty;
        if (!providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Provider hiện tại không phải PostgreSQL/Npgsql: {providerName}. Hãy kiểm tra lại AddInfrastructure.");
        }

        try
        {
            await EnsureWebPhotocopyHubTablesCreatedAsync(cancellationToken);
        }
        catch (Exception ex) when (ContainsException<SocketException>(ex))
        {
            throw new InvalidOperationException(
                "Không thể kết nối PostgreSQL/Supabase vì host không truy cập được từ máy hiện tại. " +
                "Nếu bạn đang dùng host dạng db.<project-ref>.supabase.co thì đó là Direct Connection, thường cần IPv6 hoặc Supabase IPv4 add-on. " +
                "Với máy/mạng IPv4-only, hãy dùng Shared Pooler - Session mode: postgres://postgres.<project-ref>:<password>@aws-<region>.pooler.supabase.com:5432/postgres. " +
                "Không dùng placeholder [YOUR-PASSWORD], không dùng Database=DTBWebPhotocopyHub; Supabase hosted dùng database postgres.",
                ex);
        }
        catch (Exception ex) when (LooksLikeDatabaseConnectionException(ex))
        {
            throw new InvalidOperationException(
                "Không thể khởi tạo PostgreSQL/Supabase. Hãy kiểm tra ConnectionStrings:DefaultConnection hoặc WEBPHOTOCOPYHUB_POSTGRES_CONNECTION, SSL Mode, port, user, password và trạng thái Supabase project.",
                ex);
        }
    }

    private bool IsSeedOnlyMode()
    {
        return _configuration.GetValue<bool>("WEBPHOTOCOPYHUB_SEED_ONLY")
            || _configuration.GetValue<bool>("PHOTOCOPYHUB_SEED_ONLY");
    }

    private async Task EnsureWebPhotocopyHubTablesCreatedAsync(CancellationToken cancellationToken)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

        try
        {
            await _dbContext.Set<IdentityRole>().AnyAsync(cancellationToken);
        }
        catch (Exception ex) when (LooksLikeMissingWebPhotocopyHubTableException(ex))
        {
            // ChatGPT fix 2026-06-03: Supabase has managed schemas, so EnsureCreated can skip app tables.
            _logger.LogInformation("Supabase database has existing managed tables but WebPhotocopyHub tables are missing. Creating WebPhotocopyHub tables.");

            var databaseCreator = _dbContext.GetService<IRelationalDatabaseCreator>();
            await databaseCreator.CreateTablesAsync(cancellationToken);
        }
    }

    private static bool ContainsException<TException>(Exception exception)
        where TException : Exception
    {
        return ContainsExceptionMatching(exception, item => item is TException);
    }

    private static bool LooksLikeDatabaseConnectionException(Exception exception)
    {
        return ContainsExceptionMatching(exception, item =>
        {
            var typeName = item.GetType().FullName ?? string.Empty;
            return item is TimeoutException
                || item is ArgumentException
                || typeName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                || item.Message.Contains("Host can't be null", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool LooksLikeMissingWebPhotocopyHubTableException(Exception exception)
    {
        return ContainsExceptionMatching(exception, item =>
        {
            var sqlState = item.GetType().GetProperty("SqlState")?.GetValue(item)?.ToString();
            return string.Equals(sqlState, "42P01", StringComparison.OrdinalIgnoreCase)
                && item.Message.Contains("AspNetRoles", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool ContainsExceptionMatching(Exception exception, Func<Exception, bool> predicate)
    {
        var current = exception;
        while (current is not null)
        {
            if (predicate(current))
            {
                return true;
            }

            current = current.InnerException;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                if (ContainsExceptionMatching(innerException, predicate))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void BackupIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var backupPath = $"{path}.broken-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        try
        {
            File.Move(path, backupPath, overwrite: true);
            _logger.LogWarning("Đã backup database lỗi sang: {BackupPath}", backupPath);
        }
        catch (IOException moveException)
        {
            try
            {
                File.Copy(path, backupPath, overwrite: true);
                _logger.LogWarning(moveException, "Không thể di chuyển file do đang bị khóa. Đã copy backup sang: {BackupPath}", backupPath);
            }
            catch (Exception backupException) when (backupException is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(backupException, "Không thể backup file SQLite: {Path}. Tiếp tục khởi tạo lại DB mà không backup file này.", path);
            }
        }
    }

    private string GetConfiguredValue(string key, string fallback)
    {
        var configuredValue = _configuration[key];
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return configuredValue;
        }

        return fallback;
    }
    private async Task SeedRolesAsync()
    {
        if (!await _roleManager.RoleExistsAsync(RoleConstants.Admin))
        {
            await _roleManager.CreateAsync(new IdentityRole(RoleConstants.Admin));
        }

        if (!await _roleManager.RoleExistsAsync(RoleConstants.Customer))
        {
            await _roleManager.CreateAsync(new IdentityRole(RoleConstants.Customer));
        }

        if (!await _roleManager.RoleExistsAsync(RoleConstants.ShopOperator))
        {
            await _roleManager.CreateAsync(new IdentityRole(RoleConstants.ShopOperator));
        }
    }

    private async Task SeedAdminAsync()
    {
        var email = GetConfiguredValue("SeedAdmin:Email", "admin@webphotocopyhub.local");
        var password = GetConfiguredValue("SeedAdmin:Password", "Admin@123456");
        var fullName = GetConfiguredValue("SeedAdmin:FullName", "Quản trị hệ thống");

        var existing = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existing is null)
        {
            existing = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                PhoneNumberConfirmed = true,
                CurrentBalance = 0
            };

            var result = await _userManager.CreateAsync(existing, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                _logger.LogError("Không thể tạo admin mặc định: {Errors}", errors);
                return;
            }

            _logger.LogInformation("Đã tạo admin mặc định: {Email}", email);
        }

        existing.UserName = email;
        existing.Email = email;
        existing.EmailConfirmed = true;
        existing.FullName = fullName;
        existing.IsActive = true;
        existing.LockoutEnd = null;
        existing.AccessFailedCount = 0;
        existing.PhoneNumberConfirmed = true;

        if (!await _userManager.IsInRoleAsync(existing, RoleConstants.Admin))
        {
            await _userManager.AddToRoleAsync(existing, RoleConstants.Admin);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existing);
        var resetResult = await _userManager.ResetPasswordAsync(existing, resetToken, password);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(x => x.Description));
            _logger.LogWarning("Không thể reset mật khẩu admin seed {Email}: {Errors}", email, errors);
        }

        var updateResult = await _userManager.UpdateAsync(existing);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(x => x.Description));
            _logger.LogWarning("Không thể cập nhật admin seed {Email}: {Errors}", email, errors);
        }
    }

    private async Task SeedShopOperatorAsync()
    {
        var email = GetConfiguredValue("SeedShopOperator:Email", "operator@webphotocopyhub.local");
        var password = GetConfiguredValue("SeedShopOperator:Password", "Operator@123456");
        var fullName = GetConfiguredValue("SeedShopOperator:FullName", "Nhân viên Toàn Photocopy");

        var existing = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existing is null)
        {
            existing = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                PhoneNumberConfirmed = true,
                CurrentBalance = 0
            };

            var result = await _userManager.CreateAsync(existing, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                _logger.LogError("Không thể tạo tài khoản ShopOperator mặc định: {Errors}", errors);
                return;
            }

            _logger.LogInformation("Đã tạo ShopOperator mặc định: {Email}", email);
        }

        existing.UserName = email;
        existing.Email = email;
        existing.EmailConfirmed = true;
        existing.FullName = fullName;
        existing.IsActive = true;
        existing.LockoutEnd = null;
        existing.AccessFailedCount = 0;
        existing.PhoneNumberConfirmed = true;

        if (!await _userManager.IsInRoleAsync(existing, RoleConstants.ShopOperator))
        {
            await _userManager.AddToRoleAsync(existing, RoleConstants.ShopOperator);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existing);
        var resetResult = await _userManager.ResetPasswordAsync(existing, resetToken, password);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(x => x.Description));
            _logger.LogWarning("Không thể reset mật khẩu shop operator seed {Email}: {Errors}", email, errors);
        }

        var updateResult = await _userManager.UpdateAsync(existing);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(x => x.Description));
            _logger.LogWarning("Không thể cập nhật shop operator seed {Email}: {Errors}", email, errors);
        }
    }

    private async Task SeedDefaultCustomerAccountsAsync()
    {
        var accounts = new[]
        {
            new
            {
                Email = "sinhvien01@webphotocopyhub.local",
                Password = "Student@123",
                FullName = "Nguyễn Minh Khoa",
                PhoneNumber = "0910000001",
                Address = "KTX khu A - ĐHQG TP.HCM"
            },
            new
            {
                Email = "sinhvien02@webphotocopyhub.local",
                Password = "Student@123",
                FullName = "Trần Thu Hà",
                PhoneNumber = "0910000002",
                Address = "Quận 7, TP.HCM"
            },
            new
            {
                Email = "sinhvien03@webphotocopyhub.local",
                Password = "Student@123",
                FullName = "Lê Gia Bảo",
                PhoneNumber = "0910000003",
                Address = "Thủ Đức, TP.HCM"
            },
            new
            {
                Email = "khachhang@webphotocopyhub.local",
                Password = "Customer@123",
                FullName = "Khách hàng mặc định",
                PhoneNumber = "0910000099",
                Address = "Địa chỉ nhận tài liệu sẽ cập nhật khi đặt đơn"
            }
        };

        foreach (var account in accounts)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == account.Email);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = account.Email,
                    Email = account.Email,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    FullName = account.FullName,
                    PhoneNumber = account.PhoneNumber,
                    Address = account.Address,
                    IsActive = true,
                    CurrentBalance = 0
                };

                var createResult = await _userManager.CreateAsync(user, account.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(x => x.Description));
                    _logger.LogWarning("Không thể tạo customer seed {Email}: {Errors}", account.Email, errors);
                    continue;
                }

                _logger.LogInformation("Đã tạo customer mặc định: {Email}", account.Email);
            }

            user.UserName = account.Email;
            user.Email = account.Email;
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = true;
            user.FullName = account.FullName;
            user.PhoneNumber = account.PhoneNumber;
            user.Address = account.Address;
            user.IsActive = true;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;

            if (!await _userManager.IsInRoleAsync(user, RoleConstants.Customer))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, RoleConstants.Customer);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(x => x.Description));
                    _logger.LogWarning("Không thể gán role Customer cho {Email}: {Errors}", account.Email, errors);
                }
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, account.Password);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(x => x.Description));
                _logger.LogWarning("Không thể reset mật khẩu customer seed {Email}: {Errors}", account.Email, errors);
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(x => x.Description));
                _logger.LogWarning("Không thể cập nhật customer seed {Email}: {Errors}", account.Email, errors);
            }
        }
    }
    private async Task SeedPricingAsync(CancellationToken cancellationToken)
    {
        var seedRules = new List<PricingRule>
        {
            new() { PaperSize = PaperSize.A5, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 500 },
            new() { PaperSize = PaperSize.A5, PrintSide = PrintSide.TwoSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 900 },
            new() { PaperSize = PaperSize.A5, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.Color, IsPhoto = false, UnitPrice = 1800 },
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 700 },
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.TwoSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 1200 },
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.Color, IsPhoto = false, UnitPrice = 2500 },
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.TwoSide, ColorMode = ColorMode.Color, IsPhoto = false, UnitPrice = 4500 },
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.Color, IsPhoto = true, UnitPrice = 6000 },
            new() { PaperSize = PaperSize.A3, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 2000 },
            new() { PaperSize = PaperSize.A3, PrintSide = PrintSide.TwoSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 3500 },
            new() { PaperSize = PaperSize.A3, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.Color, IsPhoto = false, UnitPrice = 5500 },
            new() { PaperSize = PaperSize.A3, PrintSide = PrintSide.TwoSide, ColorMode = ColorMode.Color, IsPhoto = false, UnitPrice = 9500 },
            new() { PaperSize = PaperSize.A0, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 25000 },
            new() { PaperSize = PaperSize.A0, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.Color, IsPhoto = false, UnitPrice = 45000 }
        };

        foreach (var seedRule in seedRules)
        {
            var existing = await _dbContext.PricingRules.FirstOrDefaultAsync(
                x => x.PaperSize == seedRule.PaperSize
                    && x.PrintSide == seedRule.PrintSide
                    && x.ColorMode == seedRule.ColorMode
                    && x.IsPhoto == seedRule.IsPhoto,
                cancellationToken);

            if (existing is null)
            {
                _dbContext.PricingRules.Add(seedRule);
                continue;
            }

            existing.UnitPrice = seedRule.UnitPrice;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        var products = new List<Product>
        {
            new() { Name = "Giấy A4 Double A (500 tờ)", Description = "Giấy in văn phòng phổ biến, định lượng 70gsm.", Price = 95000, StockQuantity = 100, IsActive = true },
            new() { Name = "Bút bi Thiên Long", Description = "Bút bi xanh dùng cho học tập và văn phòng.", Price = 5000, StockQuantity = 300, IsActive = true },
            new() { Name = "Bìa nhựa A4", Description = "Bìa hồ sơ trong suốt cho tài liệu A4.", Price = 7000, StockQuantity = 120, IsActive = true },
            new() { Name = "Giấy ảnh A4 Glossy (20 tờ)", Description = "Giấy ảnh bóng dùng cho in màu và ảnh thẻ.", Price = 45000, StockQuantity = 60, IsActive = true },
            new() { Name = "Bìa màu A4", Description = "Bìa màu làm trang bìa, phân tập tài liệu.", Price = 2500, StockQuantity = 500, IsActive = true },
            new() { Name = "Kẹp bướm 25mm", Description = "Kẹp bướm kẹp hồ sơ, tài liệu dày vừa.", Price = 12000, StockQuantity = 80, IsActive = true },
            new() { Name = "Sổ tay A5", Description = "Sổ tay ghi chú khổ A5.", Price = 28000, StockQuantity = 90, IsActive = true },
            new() { Name = "Mực dấu đỏ", Description = "Lọ mực dấu đỏ dùng cho văn phòng.", Price = 18000, StockQuantity = 45, IsActive = true }
        };

        foreach (var seedProduct in products)
        {
            var existing = await _dbContext.Products.FirstOrDefaultAsync(
                x => x.Name == seedProduct.Name,
                cancellationToken);

            if (existing is null)
            {
                _dbContext.Products.Add(seedProduct);
                continue;
            }

            existing.Description = seedProduct.Description;
            existing.Price = seedProduct.Price;
            existing.StockQuantity = seedProduct.StockQuantity;
            existing.ImageUrl = seedProduct.ImageUrl;
            existing.IsActive = seedProduct.IsActive;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSupportServicesAsync(CancellationToken cancellationToken)
    {
        var services = new List<SupportService>
        {
            new() { Name = "Đóng gáy", Description = "Đóng gáy lò xo tài liệu.", UnitPrice = 12000, FeeType = SupportFeeType.PerQuantity, IsActive = true },
            new() { Name = "Ép plastic", Description = "Ép plastic giấy tờ, thẻ và tài liệu nhỏ.", UnitPrice = 8000, FeeType = SupportFeeType.PerQuantity, IsActive = true },
            new() { Name = "Scan tài liệu", Description = "Scan tài liệu sang PDF.", UnitPrice = 3000, FeeType = SupportFeeType.PerQuantity, IsActive = true },
            new() { Name = "Đánh máy", Description = "Đánh máy văn bản cơ bản.", UnitPrice = 50000, FeeType = SupportFeeType.Fixed, IsActive = true },
            new() { Name = "Bấm kim phân tập", Description = "Bấm kim và phân tập tài liệu theo bộ.", UnitPrice = 5000, FeeType = SupportFeeType.PerQuantity, IsActive = true },
            new() { Name = "Cán màng bìa", Description = "Cán màng trang bìa hoặc tài liệu cần bảo vệ.", UnitPrice = 15000, FeeType = SupportFeeType.PerQuantity, IsActive = true },
            new() { Name = "Chỉnh file cơ bản", Description = "Căn lề, gộp file PDF và kiểm tra trước khi in.", UnitPrice = 30000, FeeType = SupportFeeType.Fixed, IsActive = true }
        };

        foreach (var seedService in services)
        {
            var existing = await _dbContext.SupportServices.FirstOrDefaultAsync(
                x => x.Name == seedService.Name,
                cancellationToken);

            if (existing is null)
            {
                _dbContext.SupportServices.Add(seedService);
                continue;
            }

            existing.Description = seedService.Description;
            existing.UnitPrice = seedService.UnitPrice;
            existing.FeeType = seedService.FeeType;
            existing.IsActive = seedService.IsActive;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private bool IsSampleSeedEnabled()
    {
        return _configuration.GetValue<bool?>("SeedSampleData:Enabled") ?? false;
    }

    private async Task SeedSampleDataAsync(CancellationToken cancellationToken)
    {
        if (!IsSampleSeedEnabled())
        {
            return;
        }

        var customers = await SeedSampleCustomersAsync();
        if (customers.Count == 0)
        {
            return;
        }

        var adminEmail = _configuration["SeedAdmin:Email"] ?? "admin@webphotocopyhub.local";
        var operatorEmail = _configuration["SeedShopOperator:Email"] ?? "operator@webphotocopyhub.local";

        var adminUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == adminEmail, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy admin seed để tạo dữ liệu mẫu.");
        var operatorUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == operatorEmail, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy shop operator seed để tạo dữ liệu mẫu.");

        await CleanupSampleTransactionalDataAsync(customers, cancellationToken);

        var runningBalancesByUserId = await GetLedgerBalancesByUserIdAsync(customers, cancellationToken);
        var uploadedFilesByUserId = await SeedSampleUploadedFilesAsync(customers, cancellationToken);
        var productsByName = await GetSeedProductsByNameAsync(cancellationToken);
        var servicesByName = await GetSeedSupportServicesByNameAsync(cancellationToken);

        SeedSampleTopUps(customers, runningBalancesByUserId, adminUser, operatorUser);
        SeedSampleStockMovements(productsByName, operatorUser);
        SeedSamplePrintJobs(customers, uploadedFilesByUserId, runningBalancesByUserId, adminUser, operatorUser);
        SeedSampleProductOrders(customers, productsByName, runningBalancesByUserId, operatorUser);
        SeedSampleSupportOrders(customers, servicesByName, runningBalancesByUserId, operatorUser);

        foreach (var customer in customers)
        {
            customer.CurrentBalance = runningBalancesByUserId.TryGetValue(customer.Id, out var balance)
                ? balance
                : 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Đã seed dữ liệu mẫu đầy đủ để test cho môi trường hiện tại.");
    }

    private async Task<List<ApplicationUser>> SeedSampleCustomersAsync()
    {
        var samples = new[]
        {
            new { Email = "sinhvien01@webphotocopyhub.local", Password = "Student@123", FullName = "Nguyễn Minh Khoa", Phone = "0910000001", Address = "KTX khu A - ĐHQG TP.HCM" },
            new { Email = "sinhvien02@webphotocopyhub.local", Password = "Student@123", FullName = "Trần Thu Hà", Phone = "0910000002", Address = "Quận 7, TP.HCM" },
            new { Email = "sinhvien03@webphotocopyhub.local", Password = "Student@123", FullName = "Lê Gia Bảo", Phone = "0910000003", Address = "Thủ Đức, TP.HCM" },
            new { Email = "khachhang@webphotocopyhub.local", Password = "Customer@123", FullName = "Khách hàng mặc định", Phone = "0910000099", Address = "Địa chỉ nhận tài liệu sẽ cập nhật khi đặt đơn" }
        };

        var users = new List<ApplicationUser>();

        foreach (var sample in samples)
        {
            var existing = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == sample.Email);
            if (existing is null)
            {
                existing = new ApplicationUser
                {
                    UserName = sample.Email,
                    Email = sample.Email,
                    EmailConfirmed = true,
                    FullName = sample.FullName,
                    PhoneNumber = sample.Phone,
                    PhoneNumberConfirmed = true,
                    Address = sample.Address,
                    IsActive = true,
                    CurrentBalance = 0
                };

                var createResult = await _userManager.CreateAsync(existing, sample.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(x => x.Description));
                    _logger.LogWarning("Không thể tạo user seed {Email}: {Errors}", sample.Email, errors);
                    continue;
                }
            }

            existing.UserName = sample.Email;
            existing.Email = sample.Email;
            existing.EmailConfirmed = true;
            existing.FullName = sample.FullName;
            existing.PhoneNumber = sample.Phone;
            existing.PhoneNumberConfirmed = true;
            existing.Address = sample.Address;
            existing.IsActive = true;
            existing.LockoutEnd = null;
            existing.AccessFailedCount = 0;

            if (!await _userManager.IsInRoleAsync(existing, RoleConstants.Customer))
            {
                await _userManager.AddToRoleAsync(existing, RoleConstants.Customer);
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existing);
            var resetResult = await _userManager.ResetPasswordAsync(existing, resetToken, sample.Password);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(x => x.Description));
                _logger.LogWarning("Không thể reset mật khẩu user seed {Email}: {Errors}", sample.Email, errors);
            }

            var updateResult = await _userManager.UpdateAsync(existing);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(x => x.Description));
                _logger.LogWarning("Không thể cập nhật user seed {Email}: {Errors}", sample.Email, errors);
            }

            users.Add(existing);
        }

        await _dbContext.SaveChangesAsync();
        return users;
    }

    private async Task CleanupSampleTransactionalDataAsync(
        IReadOnlyList<ApplicationUser> customers,
        CancellationToken cancellationToken)
    {
        var userIds = customers.Select(x => x.Id).ToList();

        var seedPrintJobs = await _dbContext.PrintJobs
            .Where(x => userIds.Contains(x.UserId)
                && x.SubmitIdempotencyKey != null
                && EF.Functions.Like(x.SubmitIdempotencyKey, "seed-%"))
            .ToListAsync(cancellationToken);
        _dbContext.PrintJobs.RemoveRange(seedPrintJobs);

        var seedProductOrders = await _dbContext.ProductOrders
            .Include(x => x.Items)
            .Where(x => userIds.Contains(x.UserId)
                && x.OrderIdempotencyKey != null
                && EF.Functions.Like(x.OrderIdempotencyKey, "seed-%"))
            .ToListAsync(cancellationToken);
        _dbContext.ProductOrders.RemoveRange(seedProductOrders);

        var seedSupportOrders = await _dbContext.SupportServiceOrders
            .Where(x => userIds.Contains(x.UserId)
                && x.OrderIdempotencyKey != null
                && EF.Functions.Like(x.OrderIdempotencyKey, "seed-%"))
            .ToListAsync(cancellationToken);
        _dbContext.SupportServiceOrders.RemoveRange(seedSupportOrders);

        var seedTopUps = await _dbContext.TopUpRequests
            .Where(x => userIds.Contains(x.UserId)
                && x.CreateIdempotencyKey != null
                && EF.Functions.Like(x.CreateIdempotencyKey, "seed-%"))
            .ToListAsync(cancellationToken);
        _dbContext.TopUpRequests.RemoveRange(seedTopUps);

        var seedWalletTransactions = await _dbContext.WalletTransactions
            .Where(x => userIds.Contains(x.UserId)
                && x.IdempotencyKey != null
                && EF.Functions.Like(x.IdempotencyKey, "seed-%"))
            .ToListAsync(cancellationToken);
        _dbContext.WalletTransactions.RemoveRange(seedWalletTransactions);

        var seedStockMovements = await _dbContext.ProductStockMovements
            .Where(x => x.Note != null && EF.Functions.Like(x.Note, "Seed:%"))
            .ToListAsync(cancellationToken);
        _dbContext.ProductStockMovements.RemoveRange(seedStockMovements);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, decimal>> GetLedgerBalancesByUserIdAsync(
        IReadOnlyList<ApplicationUser> customers,
        CancellationToken cancellationToken)
    {
        var userIds = customers.Select(x => x.Id).ToList();
        var balances = await _dbContext.WalletTransactions
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(x => new
            {
                UserId = x.Key,
                Balance = x.Sum(y => y.Amount)
            })
            .ToDictionaryAsync(x => x.UserId, x => x.Balance, cancellationToken);

        foreach (var customer in customers)
        {
            balances.TryAdd(customer.Id, 0);
        }

        return balances;
    }

    private async Task<Dictionary<string, UploadedFileMetadata>> SeedSampleUploadedFilesAsync(
        IReadOnlyList<ApplicationUser> customers,
        CancellationToken cancellationToken)
    {
        var rootPathSetting = _configuration["FileStorage:RootPath"] ?? "App_Data/uploads";
        var fileRoot = Path.IsPathRooted(rootPathSetting)
            ? rootPathSetting
            : Path.Combine(_hostEnvironment.ContentRootPath, rootPathSetting);

        var seedDir = Path.Combine(fileRoot, "seed");
        Directory.CreateDirectory(seedDir);

        var now = DateTime.UtcNow;
        var result = new Dictionary<string, UploadedFileMetadata>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < customers.Count; i++)
        {
            var customer = customers[i];
            var storedFileName = $"seed-order-{i + 1}.pdf";
            var relativePath = $"seed/{storedFileName}";
            var fullPath = Path.Combine(seedDir, storedFileName);

            if (!File.Exists(fullPath))
            {
                await File.WriteAllBytesAsync(fullPath, SeedPdfContent, cancellationToken);
            }

            var metadata = await _dbContext.UploadedFileMetadatas.FirstOrDefaultAsync(
                x => x.OwnerUserId == customer.Id && x.StoredFileName == storedFileName,
                cancellationToken);

            if (metadata is null)
            {
                metadata = new UploadedFileMetadata
                {
                    OwnerUserId = customer.Id,
                    OriginalFileName = $"TaiLieuMonHoc-{i + 1}.pdf",
                    StoredFileName = storedFileName,
                    RelativePath = relativePath,
                    Size = SeedPdfContent.LongLength,
                    ContentType = "application/pdf",
                    IsForPrintJob = true,
                    CreatedAt = now.AddDays(-(8 - i))
                };
                _dbContext.UploadedFileMetadatas.Add(metadata);
            }
            else
            {
                metadata.OriginalFileName = $"TaiLieuMonHoc-{i + 1}.pdf";
                metadata.RelativePath = relativePath;
                metadata.Size = SeedPdfContent.LongLength;
                metadata.ContentType = "application/pdf";
                metadata.IsForPrintJob = true;
            }

            result[customer.Id] = metadata;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<Dictionary<string, Product>> GetSeedProductsByNameAsync(CancellationToken cancellationToken)
    {
        var seedProductNames = new[]
        {
            "Giấy A4 Double A (500 tờ)",
            "Bút bi Thiên Long",
            "Bìa nhựa A4",
            "Giấy ảnh A4 Glossy (20 tờ)",
            "Bìa màu A4",
            "Kẹp bướm 25mm",
            "Sổ tay A5",
            "Mực dấu đỏ"
        };

        var seedProductNameSet = seedProductNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var products = await _dbContext.Products.ToListAsync(cancellationToken);

        return products
            .Where(x => seedProductNameSet.Contains(x.Name))
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, SupportService>> GetSeedSupportServicesByNameAsync(CancellationToken cancellationToken)
    {
        var seedServiceNames = new[]
        {
            "Đóng gáy",
            "Ép plastic",
            "Scan tài liệu",
            "Đánh máy",
            "Bấm kim phân tập",
            "Cán màng bìa",
            "Chỉnh file cơ bản"
        };

        var seedServiceNameSet = seedServiceNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var services = await _dbContext.SupportServices.ToListAsync(cancellationToken);

        return services
            .Where(x => seedServiceNameSet.Contains(x.Name))
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    private void SeedSampleTopUps(
        IReadOnlyList<ApplicationUser> customers,
        IDictionary<string, decimal> runningBalancesByUserId,
        ApplicationUser adminUser,
        ApplicationUser operatorUser)
    {
        var now = DateTime.UtcNow;
        var usersByEmail = customers.ToDictionary(x => x.Email ?? x.UserName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        var sinhVien01 = usersByEmail["sinhvien01@webphotocopyhub.local"];
        var topUp01 = new TopUpRequest
        {
            UserId = sinhVien01.Id,
            Amount = 300000,
            TransferContent = "TOPUP SV01",
            TransactionReferenceCode = "SEED-VCB-0001",
            CreateIdempotencyKey = "seed-topup-u1-approved-001",
            LastReviewIdempotencyKey = "seed-review-u1-approved-001",
            Channel = TopUpChannel.BankTransfer,
            Status = TopUpStatus.Approved,
            RequiresAdminApproval = false,
            ReviewedByAdminId = operatorUser.Id,
            ReviewedAt = now.AddDays(-8),
            ReviewNote = "Seed: Đã duyệt nạp ví 300.000",
            CreatedAt = now.AddDays(-8)
        };
        _dbContext.TopUpRequests.Add(topUp01);
        var topUpTx01 = AddSeedWalletTransaction(
            sinhVien01,
            runningBalancesByUserId,
            WalletTransactionType.TopUpApproved,
            300000,
            "seed-wallet-u1-topup-approved-001",
            "Seed: Nạp ví đã duyệt cho sinh viên 01",
            nameof(TopUpRequest),
            topUp01.Id,
            now.AddDays(-8).AddMinutes(5),
            operatorUser.Id);
        topUp01.ApprovedWalletTransactionId = topUpTx01.Id;

        var sinhVien02 = usersByEmail["sinhvien02@webphotocopyhub.local"];
        var topUp02Approved = new TopUpRequest
        {
            UserId = sinhVien02.Id,
            Amount = 180000,
            TransferContent = "TOPUP SV02",
            TransactionReferenceCode = "SEED-VCB-0002",
            CreateIdempotencyKey = "seed-topup-u2-approved-001",
            LastReviewIdempotencyKey = "seed-review-u2-approved-001",
            Channel = TopUpChannel.BankTransfer,
            Status = TopUpStatus.Approved,
            RequiresAdminApproval = false,
            ReviewedByAdminId = operatorUser.Id,
            ReviewedAt = now.AddDays(-6),
            ReviewNote = "Seed: Đã duyệt nạp ví 180.000",
            CreatedAt = now.AddDays(-6)
        };
        _dbContext.TopUpRequests.Add(topUp02Approved);
        var topUpTx02 = AddSeedWalletTransaction(
            sinhVien02,
            runningBalancesByUserId,
            WalletTransactionType.TopUpApproved,
            180000,
            "seed-wallet-u2-topup-approved-001",
            "Seed: Nạp ví đã duyệt cho sinh viên 02",
            nameof(TopUpRequest),
            topUp02Approved.Id,
            now.AddDays(-6).AddMinutes(5),
            operatorUser.Id);
        topUp02Approved.ApprovedWalletTransactionId = topUpTx02.Id;

        var topUp02Pending = new TopUpRequest
        {
            UserId = sinhVien02.Id,
            Amount = 150000,
            TransferContent = "TOPUP SV02 CHO DUYET",
            TransactionReferenceCode = "SEED-VCB-0003",
            CreateIdempotencyKey = "seed-topup-u2-pending-001",
            Channel = TopUpChannel.BankTransfer,
            Status = TopUpStatus.Pending,
            RequiresAdminApproval = false,
            CreatedAt = now.AddHours(-20)
        };
        _dbContext.TopUpRequests.Add(topUp02Pending);
        AddSeedWalletTransaction(
            sinhVien02,
            runningBalancesByUserId,
            WalletTransactionType.TopUpPending,
            0,
            "seed-wallet-u2-topup-pending-001",
            "Seed: Yêu cầu nạp ví đang chờ duyệt",
            nameof(TopUpRequest),
            topUp02Pending.Id,
            now.AddHours(-20),
            null);

        var sinhVien03 = usersByEmail["sinhvien03@webphotocopyhub.local"];
        var topUp03Approved = new TopUpRequest
        {
            UserId = sinhVien03.Id,
            Amount = 220000,
            TransferContent = "TOPUP SV03",
            TransactionReferenceCode = "SEED-VCB-0004",
            CreateIdempotencyKey = "seed-topup-u3-approved-001",
            LastReviewIdempotencyKey = "seed-review-u3-approved-001",
            Channel = TopUpChannel.BankTransfer,
            Status = TopUpStatus.Approved,
            RequiresAdminApproval = false,
            ReviewedByAdminId = operatorUser.Id,
            ReviewedAt = now.AddDays(-4),
            ReviewNote = "Seed: Đã duyệt nạp ví 220.000",
            CreatedAt = now.AddDays(-4)
        };
        _dbContext.TopUpRequests.Add(topUp03Approved);
        var topUpTx03 = AddSeedWalletTransaction(
            sinhVien03,
            runningBalancesByUserId,
            WalletTransactionType.TopUpApproved,
            220000,
            "seed-wallet-u3-topup-approved-001",
            "Seed: Nạp ví đã duyệt cho sinh viên 03",
            nameof(TopUpRequest),
            topUp03Approved.Id,
            now.AddDays(-4).AddMinutes(5),
            operatorUser.Id);
        topUp03Approved.ApprovedWalletTransactionId = topUpTx03.Id;

        var topUp03Large = new TopUpRequest
        {
            UserId = sinhVien03.Id,
            Amount = 2500000,
            TransferContent = "TOPUP SV03 LON",
            TransactionReferenceCode = "SEED-VCB-0005",
            CreateIdempotencyKey = "seed-topup-u3-pending-admin-001",
            LastReviewIdempotencyKey = "seed-review-u3-pending-admin-001",
            Channel = TopUpChannel.BankTransfer,
            Status = TopUpStatus.PendingAdminApproval,
            RequiresAdminApproval = true,
            ReviewedByAdminId = operatorUser.Id,
            ReviewedAt = now.AddHours(-12),
            ReviewNote = "Seed: ShopOperator đã duyệt bước 1, chờ Admin duyệt bước 2",
            CreatedAt = now.AddDays(-1)
        };
        _dbContext.TopUpRequests.Add(topUp03Large);
        AddSeedWalletTransaction(
            sinhVien03,
            runningBalancesByUserId,
            WalletTransactionType.TopUpPending,
            0,
            "seed-wallet-u3-topup-pending-admin-001",
            "Seed: Yêu cầu nạp lớn đang chờ admin duyệt bước 2",
            nameof(TopUpRequest),
            topUp03Large.Id,
            now.AddHours(-12),
            operatorUser.Id);

        var khachHang = usersByEmail["khachhang@webphotocopyhub.local"];
        var rejectedTopUp = new TopUpRequest
        {
            UserId = khachHang.Id,
            Amount = 50000,
            TransferContent = "TOPUP KHACHHANG SAI NOI DUNG",
            TransactionReferenceCode = "SEED-VCB-0006",
            CreateIdempotencyKey = "seed-topup-u4-rejected-001",
            LastReviewIdempotencyKey = "seed-review-u4-rejected-001",
            Channel = TopUpChannel.BankTransfer,
            Status = TopUpStatus.Rejected,
            RequiresAdminApproval = false,
            ReviewedByAdminId = adminUser.Id,
            ReviewedAt = now.AddDays(-2),
            ReviewNote = "Seed: Từ chối vì nội dung chuyển khoản không khớp",
            CreatedAt = now.AddDays(-3)
        };
        _dbContext.TopUpRequests.Add(rejectedTopUp);
        AddSeedWalletTransaction(
            khachHang,
            runningBalancesByUserId,
            WalletTransactionType.TopUpRejected,
            0,
            "seed-wallet-u4-topup-rejected-001",
            "Seed: Từ chối yêu cầu nạp ví",
            nameof(TopUpRequest),
            rejectedTopUp.Id,
            now.AddDays(-2),
            adminUser.Id);
    }

    private void SeedSampleStockMovements(
        IReadOnlyDictionary<string, Product> productsByName,
        ApplicationUser operatorUser)
    {
        var createdAt = DateTime.UtcNow.AddDays(-9);

        foreach (var product in productsByName.Values.OrderBy(x => x.Name))
        {
            _dbContext.ProductStockMovements.Add(new ProductStockMovement
            {
                ProductId = product.Id,
                ActorUserId = operatorUser.Id,
                MovementType = StockMovementType.Restock,
                QuantityChanged = product.StockQuantity,
                StockBefore = 0,
                StockAfter = product.StockQuantity,
                Note = "Seed: Nhập tồn đầu kỳ cho dữ liệu mẫu",
                CreatedAt = createdAt
            });
        }
    }

    private void SeedSamplePrintJobs(
        IReadOnlyList<ApplicationUser> customers,
        IReadOnlyDictionary<string, UploadedFileMetadata> uploadedFilesByUserId,
        IDictionary<string, decimal> runningBalancesByUserId,
        ApplicationUser adminUser,
        ApplicationUser operatorUser)
    {
        var now = DateTime.UtcNow;
        var shippingFee = _configuration.GetValue<decimal?>("Business:ShippingFee") ?? 15000;
        var usersByEmail = customers.ToDictionary(x => x.Email ?? x.UserName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        var sinhVien01 = usersByEmail["sinhvien01@webphotocopyhub.local"];
        var job01 = new PrintJob
        {
            UserId = sinhVien01.Id,
            UploadedFileId = uploadedFilesByUserId[sinhVien01.Id].Id,
            PaperSize = PaperSize.A4,
            PrintSide = PrintSide.TwoSide,
            ColorMode = ColorMode.BlackWhite,
            IsPhoto = false,
            Copies = 2,
            TotalPages = 32,
            Notes = "Seed: In đóng tập cho môn Kinh tế vi mô.",
            DeliveryMethod = DeliveryMethod.PickupAtStore,
            UnitPrice = 1200,
            SubTotal = 76800,
            ShippingFee = 0,
            TotalAmount = 76800,
            Status = PrintJobStatus.Completed,
            ConfirmedByOperatorId = operatorUser.Id,
            ConfirmedAt = now.AddDays(-7),
            AssignedOperatorId = operatorUser.Id,
            LastStatusNote = "Seed: Đã in xong và khách đã nhận.",
            PaidAt = now.AddDays(-7).AddHours(1),
            ProcessedByAdminId = adminUser.Id,
            SubmitIdempotencyKey = "seed-print-u1-completed-001",
            CreatedAt = now.AddDays(-7)
        };
        _dbContext.PrintJobs.Add(job01);
        var job01Payment = AddSeedWalletTransaction(
            sinhVien01,
            runningBalancesByUserId,
            WalletTransactionType.DebitForOrder,
            -job01.TotalAmount,
            "seed-wallet-u1-print-completed-001",
            "Seed: Thanh toán đơn in đã hoàn tất",
            nameof(PrintJob),
            job01.Id,
            now.AddDays(-7).AddHours(1),
            operatorUser.Id);
        job01.PaidWalletTransactionId = job01Payment.Id;

        var sinhVien02 = usersByEmail["sinhvien02@webphotocopyhub.local"];
        var job02 = new PrintJob
        {
            UserId = sinhVien02.Id,
            UploadedFileId = uploadedFilesByUserId[sinhVien02.Id].Id,
            PaperSize = PaperSize.A4,
            PrintSide = PrintSide.OneSide,
            ColorMode = ColorMode.Color,
            IsPhoto = false,
            Copies = 1,
            TotalPages = 24,
            Notes = "Seed: Trang 1 và trang kết luận cần màu sắc rõ.",
            DeliveryMethod = DeliveryMethod.Shipping,
            DeliveryAddress = "Quận 7, TP.HCM",
            UnitPrice = 2500,
            SubTotal = 60000,
            ShippingFee = shippingFee,
            TotalAmount = 60000 + shippingFee,
            Status = PrintJobStatus.Processing,
            ConfirmedByOperatorId = operatorUser.Id,
            ConfirmedAt = now.AddDays(-2),
            AssignedOperatorId = operatorUser.Id,
            LastStatusNote = "Seed: Đang in lô 2.",
            PaidAt = now.AddDays(-2).AddHours(1),
            ProcessedByAdminId = operatorUser.Id,
            SubmitIdempotencyKey = "seed-print-u2-processing-001",
            CreatedAt = now.AddDays(-2)
        };
        _dbContext.PrintJobs.Add(job02);
        var job02Payment = AddSeedWalletTransaction(
            sinhVien02,
            runningBalancesByUserId,
            WalletTransactionType.DebitForOrder,
            -job02.TotalAmount,
            "seed-wallet-u2-print-processing-001",
            "Seed: Thanh toán đơn in đang xử lý",
            nameof(PrintJob),
            job02.Id,
            now.AddDays(-2).AddHours(1),
            operatorUser.Id);
        job02.PaidWalletTransactionId = job02Payment.Id;

        var sinhVien03 = usersByEmail["sinhvien03@webphotocopyhub.local"];
        _dbContext.PrintJobs.Add(new PrintJob
        {
            UserId = sinhVien03.Id,
            UploadedFileId = uploadedFilesByUserId[sinhVien03.Id].Id,
            PaperSize = PaperSize.A3,
            PrintSide = PrintSide.OneSide,
            ColorMode = ColorMode.BlackWhite,
            IsPhoto = false,
            Copies = 1,
            TotalPages = 10,
            Notes = "Seed: In khổ A3 dùng cho đồ án.",
            DeliveryMethod = DeliveryMethod.PickupAtStore,
            UnitPrice = 2000,
            SubTotal = 20000,
            ShippingFee = 0,
            TotalAmount = 20000,
            Status = PrintJobStatus.ConfirmedByShop,
            ConfirmedByOperatorId = operatorUser.Id,
            ConfirmedAt = now.AddHours(-8),
            AssignedOperatorId = operatorUser.Id,
            LastStatusNote = "Seed: Đã xác nhận file, chờ thanh toán.",
            ProcessedByAdminId = operatorUser.Id,
            SubmitIdempotencyKey = "seed-print-u3-confirmed-001",
            CreatedAt = now.AddHours(-10)
        });

        var khachHang = usersByEmail["khachhang@webphotocopyhub.local"];
        _dbContext.PrintJobs.Add(new PrintJob
        {
            UserId = khachHang.Id,
            UploadedFileId = uploadedFilesByUserId[khachHang.Id].Id,
            PaperSize = PaperSize.A5,
            PrintSide = PrintSide.OneSide,
            ColorMode = ColorMode.BlackWhite,
            IsPhoto = false,
            Copies = 1,
            TotalPages = 12,
            Notes = "Seed: Đơn mới gửi, shop chưa xác nhận.",
            DeliveryMethod = DeliveryMethod.PickupAtStore,
            UnitPrice = 500,
            SubTotal = 6000,
            ShippingFee = 0,
            TotalAmount = 6000,
            Status = PrintJobStatus.Submitted,
            LastStatusNote = "Seed: Chờ shop kiểm tra file.",
            SubmitIdempotencyKey = "seed-print-u4-submitted-001",
            CreatedAt = now.AddHours(-3)
        });
    }

    private void SeedSampleProductOrders(
        IReadOnlyList<ApplicationUser> customers,
        IReadOnlyDictionary<string, Product> productsByName,
        IDictionary<string, decimal> runningBalancesByUserId,
        ApplicationUser operatorUser)
    {
        var now = DateTime.UtcNow;
        var usersByEmail = customers.ToDictionary(x => x.Email ?? x.UserName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        CreateSeedProductOrder(
            user: usersByEmail["sinhvien01@webphotocopyhub.local"],
            productsByName: productsByName,
            runningBalancesByUserId: runningBalancesByUserId,
            orderIdempotencyKey: "seed-product-u1-processing-001",
            walletIdempotencyKey: "seed-wallet-u1-product-processing-001",
            deliveryMethod: DeliveryMethod.PickupAtStore,
            deliveryAddress: null,
            status: OrderStatus.Processing,
            processNote: "Seed: Đang chuẩn bị hàng văn phòng phẩm.",
            processedByOperatorId: operatorUser.Id,
            createdAt: now.AddDays(-5),
            itemSeeds: new List<(string ProductName, int Quantity)>
            {
                ("Giấy A4 Double A (500 tờ)", 1),
                ("Bút bi Thiên Long", 4)
            });

        CreateSeedProductOrder(
            user: usersByEmail["sinhvien03@webphotocopyhub.local"],
            productsByName: productsByName,
            runningBalancesByUserId: runningBalancesByUserId,
            orderIdempotencyKey: "seed-product-u3-completed-001",
            walletIdempotencyKey: "seed-wallet-u3-product-completed-001",
            deliveryMethod: DeliveryMethod.PickupAtStore,
            deliveryAddress: null,
            status: OrderStatus.Completed,
            processNote: "Seed: Đã giao văn phòng phẩm tại quầy.",
            processedByOperatorId: operatorUser.Id,
            createdAt: now.AddDays(-2),
            itemSeeds: new List<(string ProductName, int Quantity)>
            {
                ("Bút bi Thiên Long", 2),
                ("Bìa nhựa A4", 3),
                ("Kẹp bướm 25mm", 2)
            });
    }

    private void SeedSampleSupportOrders(
        IReadOnlyList<ApplicationUser> customers,
        IReadOnlyDictionary<string, SupportService> servicesByName,
        IDictionary<string, decimal> runningBalancesByUserId,
        ApplicationUser operatorUser)
    {
        var now = DateTime.UtcNow;
        var usersByEmail = customers.ToDictionary(x => x.Email ?? x.UserName ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        CreateSeedSupportOrder(
            user: usersByEmail["sinhvien01@webphotocopyhub.local"],
            service: servicesByName["Scan tài liệu"],
            runningBalancesByUserId: runningBalancesByUserId,
            orderIdempotencyKey: "seed-support-u1-completed-001",
            walletIdempotencyKey: "seed-wallet-u1-support-completed-001",
            quantity: 10,
            status: OrderStatus.Completed,
            processNote: "Seed: Đã scan và gửi file PDF.",
            processedByOperatorId: operatorUser.Id,
            createdAt: now.AddDays(-6),
            notes: "Seed: Scan tài liệu tham khảo thành PDF.");

        CreateSeedSupportOrder(
            user: usersByEmail["sinhvien02@webphotocopyhub.local"],
            service: servicesByName["Đóng gáy"],
            runningBalancesByUserId: runningBalancesByUserId,
            orderIdempotencyKey: "seed-support-u2-submitted-001",
            walletIdempotencyKey: "seed-wallet-u2-support-submitted-001",
            quantity: 2,
            status: OrderStatus.Submitted,
            processNote: null,
            processedByOperatorId: null,
            createdAt: now.AddDays(-1),
            notes: "Seed: Cần đóng gáy tài liệu nộp lớp.");
    }

    private void CreateSeedProductOrder(
        ApplicationUser user,
        IReadOnlyDictionary<string, Product> productsByName,
        IDictionary<string, decimal> runningBalancesByUserId,
        string orderIdempotencyKey,
        string walletIdempotencyKey,
        DeliveryMethod deliveryMethod,
        string? deliveryAddress,
        OrderStatus status,
        string? processNote,
        string? processedByOperatorId,
        DateTime createdAt,
        IReadOnlyList<(string ProductName, int Quantity)> itemSeeds)
    {
        var order = new ProductOrder
        {
            UserId = user.Id,
            DeliveryMethod = deliveryMethod,
            DeliveryAddress = deliveryAddress,
            Notes = "Seed: Đơn văn phòng phẩm mẫu",
            OrderIdempotencyKey = orderIdempotencyKey,
            Status = status,
            ProcessedByOperatorId = processedByOperatorId,
            ProcessedAt = processedByOperatorId is null ? null : createdAt.AddHours(2),
            ProcessNote = processNote,
            CreatedAt = createdAt
        };

        foreach (var item in itemSeeds)
        {
            if (!productsByName.TryGetValue(item.ProductName, out var product))
            {
                throw new InvalidOperationException($"Không tìm thấy sản phẩm seed: {item.ProductName}.");
            }

            var stockBefore = product.StockQuantity;
            var stockAfter = stockBefore - item.Quantity;
            if (stockAfter < 0)
            {
                throw new InvalidOperationException($"Sản phẩm seed không đủ tồn kho: {item.ProductName}.");
            }

            product.StockQuantity = stockAfter;
            order.Items.Add(new ProductOrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                LineTotal = product.Price * item.Quantity,
                CreatedAt = createdAt
            });

            _dbContext.ProductStockMovements.Add(new ProductStockMovement
            {
                ProductId = product.Id,
                ActorUserId = processedByOperatorId ?? user.Id,
                MovementType = StockMovementType.OrderDeduction,
                QuantityChanged = -item.Quantity,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                Note = $"Seed: Trừ tồn do đơn văn phòng phẩm {orderIdempotencyKey}",
                CreatedAt = createdAt.AddMinutes(10)
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.LineTotal);
        _dbContext.ProductOrders.Add(order);

        AddSeedWalletTransaction(
            user,
            runningBalancesByUserId,
            WalletTransactionType.DebitForOrder,
            -order.TotalAmount,
            walletIdempotencyKey,
            $"Seed: Thanh toán đơn văn phòng phẩm {orderIdempotencyKey}",
            nameof(ProductOrder),
            order.Id,
            createdAt.AddMinutes(15),
            processedByOperatorId);
    }

    private void CreateSeedSupportOrder(
        ApplicationUser user,
        SupportService service,
        IDictionary<string, decimal> runningBalancesByUserId,
        string orderIdempotencyKey,
        string walletIdempotencyKey,
        int quantity,
        OrderStatus status,
        string? processNote,
        string? processedByOperatorId,
        DateTime createdAt,
        string notes)
    {
        var totalAmount = service.FeeType == SupportFeeType.Fixed
            ? service.UnitPrice
            : service.UnitPrice * quantity;

        var order = new SupportServiceOrder
        {
            UserId = user.Id,
            SupportServiceId = service.Id,
            Quantity = quantity,
            UnitPrice = service.UnitPrice,
            TotalAmount = totalAmount,
            Notes = notes,
            OrderIdempotencyKey = orderIdempotencyKey,
            Status = status,
            ProcessedByOperatorId = processedByOperatorId,
            ProcessedAt = processedByOperatorId is null ? null : createdAt.AddHours(1),
            ProcessNote = processNote,
            CreatedAt = createdAt
        };

        _dbContext.SupportServiceOrders.Add(order);
        AddSeedWalletTransaction(
            user,
            runningBalancesByUserId,
            WalletTransactionType.DebitForOrder,
            -order.TotalAmount,
            walletIdempotencyKey,
            $"Seed: Thanh toán dịch vụ hỗ trợ {orderIdempotencyKey}",
            nameof(SupportServiceOrder),
            order.Id,
            createdAt.AddMinutes(10),
            processedByOperatorId);
    }

    private WalletTransaction AddSeedWalletTransaction(
        ApplicationUser user,
        IDictionary<string, decimal> runningBalancesByUserId,
        WalletTransactionType transactionType,
        decimal signedAmount,
        string idempotencyKey,
        string note,
        string referenceType,
        Guid? referenceId,
        DateTime createdAt,
        string? performedByAdminId)
    {
        if (!runningBalancesByUserId.TryGetValue(user.Id, out var balanceBefore))
        {
            balanceBefore = 0;
        }

        var balanceAfter = balanceBefore + signedAmount;
        if (balanceAfter < 0)
        {
            throw new InvalidOperationException($"Seed ví tạo số dư âm cho user {user.Email}.");
        }

        var transaction = new WalletTransaction
        {
            UserId = user.Id,
            TransactionType = transactionType,
            Amount = signedAmount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Note = note,
            IdempotencyKey = idempotencyKey,
            PerformedByAdminId = performedByAdminId,
            CreatedAt = createdAt
        };

        _dbContext.WalletTransactions.Add(transaction);
        runningBalancesByUserId[user.Id] = balanceAfter;
        return transaction;
    }
}
