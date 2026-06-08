using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Infrastructure.Data;

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
        }
    }

    private async Task EnsureDatabaseReadyAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsMySql())
        {
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        try
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }
        catch (Exception ex) when (_hostEnvironment.IsDevelopment() && _dbContext.Database.IsSqlite())
        {
            _logger.LogWarning(ex, "Migrate SQLite thất bại trong môi trường Development. Thử backup DB cũ và tạo lại.");
        }

        var dataSource = _dbContext.Database.GetDbConnection().DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new InvalidOperationException("Không xác định được đường dẫn SQLite database.");
        }

        var dbPath = Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.GetFullPath(dataSource, _hostEnvironment.ContentRootPath);

        await _dbContext.Database.CloseConnectionAsync();
        SqliteConnection.ClearAllPools();

        BackupIfExists(dbPath);
        BackupIfExists($"{dbPath}-wal");
        BackupIfExists($"{dbPath}-shm");

        try
        {
            await _dbContext.Database.EnsureDeletedAsync(cancellationToken);
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception recreateEx) when (_hostEnvironment.IsDevelopment() && _dbContext.Database.IsSqlite())
        {
            _logger.LogWarning(
                recreateEx,
                "Không thể reset SQLite sau khi migrate lỗi. Thử fallback bằng EnsureCreated để ứng dụng không bị dừng.");
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
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
        var email = _configuration["SeedAdmin:Email"] ?? "admin@photocopyhub.local";
        var password = _configuration["SeedAdmin:Password"] ?? "Admin@123456";
        var fullName = _configuration["SeedAdmin:FullName"] ?? "Quản trị hệ thống";

        var existing = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existing is null)
        {
            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                PhoneNumberConfirmed = true,
                CurrentBalance = 0
            };

            var result = await _userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                _logger.LogError("Không thể tạo admin mặc định: {Errors}", errors);
                return;
            }

            await _userManager.AddToRoleAsync(admin, RoleConstants.Admin);
            _logger.LogInformation("Đã tạo admin mặc định: {Email}", email);
            return;
        }

        if (!await _userManager.IsInRoleAsync(existing, RoleConstants.Admin))
        {
            await _userManager.AddToRoleAsync(existing, RoleConstants.Admin);
        }
    }

    private async Task SeedShopOperatorAsync()
    {
        var email = _configuration["SeedShopOperator:Email"] ?? "operator@photocopyhub.local";
        var password = _configuration["SeedShopOperator:Password"] ?? "Operator@123456";
        var fullName = _configuration["SeedShopOperator:FullName"] ?? "Nhân viên vận hành";

        var existing = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existing is null)
        {
            var op = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                PhoneNumberConfirmed = true,
                CurrentBalance = 0
            };

            var result = await _userManager.CreateAsync(op, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                _logger.LogError("Không thể tạo tài khoản ShopOperator mặc định: {Errors}", errors);
                return;
            }

            await _userManager.AddToRoleAsync(op, RoleConstants.ShopOperator);
            _logger.LogInformation("Đã tạo ShopOperator mặc định: {Email}", email);
            return;
        }

        if (!await _userManager.IsInRoleAsync(existing, RoleConstants.ShopOperator))
        {
            await _userManager.AddToRoleAsync(existing, RoleConstants.ShopOperator);
        }
    }

    private async Task SeedPricingAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.PricingRules.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedRules = new List<PricingRule>
        {
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 700 },
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.TwoSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 1200 },
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.Color, IsPhoto = false, UnitPrice = 2500 },
            new() { PaperSize = PaperSize.A3, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 2000 },
            new() { PaperSize = PaperSize.A3, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.Color, IsPhoto = false, UnitPrice = 5500 },
            new() { PaperSize = PaperSize.A4, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.Color, IsPhoto = true, UnitPrice = 6000 },
            new() { PaperSize = PaperSize.A5, PrintSide = PrintSide.OneSide, ColorMode = ColorMode.BlackWhite, IsPhoto = false, UnitPrice = 500 }
        };

        await _dbContext.PricingRules.AddRangeAsync(seedRules, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = new List<Product>
        {
            new() { Name = "Giấy A4 Double A (500 tờ)", Description = "Giấy in văn phòng phổ biến", Price = 95000, StockQuantity = 100, IsActive = true },
            new() { Name = "Bút bi Thiên Long", Description = "Bút bi xanh", Price = 5000, StockQuantity = 300, IsActive = true },
            new() { Name = "Bìa nhựa A4", Description = "Bìa hồ sơ trong suốt", Price = 7000, StockQuantity = 120, IsActive = true }
        };

        await _dbContext.Products.AddRangeAsync(products, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSupportServicesAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.SupportServices.AnyAsync(cancellationToken))
        {
            return;
        }

        var services = new List<SupportService>
        {
            new() { Name = "Đóng gáy", Description = "Đóng gáy lò xo tài liệu", UnitPrice = 12000, FeeType = SupportFeeType.PerQuantity, IsActive = true },
            new() { Name = "Ép plastic", Description = "Ép plastic giấy tờ", UnitPrice = 8000, FeeType = SupportFeeType.PerQuantity, IsActive = true },
            new() { Name = "Scan tài liệu", Description = "Scan sang PDF", UnitPrice = 3000, FeeType = SupportFeeType.PerQuantity, IsActive = true },
            new() { Name = "Đánh máy", Description = "Đánh máy văn bản cơ bản", UnitPrice = 50000, FeeType = SupportFeeType.Fixed, IsActive = true }
        };

        await _dbContext.SupportServices.AddRangeAsync(services, cancellationToken);
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

        var adminEmail = _configuration["SeedAdmin:Email"] ?? "admin@photocopyhub.local";
        var operatorEmail = _configuration["SeedShopOperator:Email"] ?? "operator@photocopyhub.local";

        var adminUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == adminEmail, cancellationToken);
        var operatorUser = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == operatorEmail, cancellationToken);

        var uploadedFilesByUserId = await SeedSampleUploadedFilesAsync(customers, cancellationToken);

        await SeedSampleWalletAndTopUpsAsync(customers, adminUser, operatorUser, cancellationToken);
        await SeedSamplePrintJobsAsync(customers, uploadedFilesByUserId, adminUser, operatorUser, cancellationToken);
        await SeedSampleProductOrdersAsync(customers, operatorUser, cancellationToken);
        await SeedSampleSupportOrdersAsync(customers, operatorUser, cancellationToken);

        _logger.LogInformation("Đã seed dữ liệu mẫu để test cho môi trường hiện tại.");
    }

    private async Task<List<ApplicationUser>> SeedSampleCustomersAsync()
    {
        var samples = new[]
        {
            new { Email = "sinhvien01@photocopyhub.local", Password = "Student@123", FullName = "Nguyễn Minh Khoa", Phone = "0910000001", Address = "KTX khu A - ĐHQG TP.HCM" },
            new { Email = "sinhvien02@photocopyhub.local", Password = "Student@123", FullName = "Trần Thu Hà", Phone = "0910000002", Address = "Quận 7, TP.HCM" },
            new { Email = "sinhvien03@photocopyhub.local", Password = "Student@123", FullName = "Lê Gia Bảo", Phone = "0910000003", Address = "Thủ Đức, TP.HCM" }
        };

        var users = new List<ApplicationUser>();

        foreach (var sample in samples)
        {
            var existing = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == sample.Email);
            if (existing is null)
            {
                var user = new ApplicationUser
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

                var createResult = await _userManager.CreateAsync(user, sample.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(x => x.Description));
                    _logger.LogWarning("Không thể tạo user seed {Email}: {Errors}", sample.Email, errors);
                    continue;
                }

                existing = user;
            }
            else
            {
                existing.IsActive = true;
                existing.EmailConfirmed = true;
                existing.PhoneNumberConfirmed = true;
                existing.FullName = string.IsNullOrWhiteSpace(existing.FullName) ? sample.FullName : existing.FullName;
                existing.PhoneNumber = string.IsNullOrWhiteSpace(existing.PhoneNumber) ? sample.Phone : existing.PhoneNumber;
                existing.Address = string.IsNullOrWhiteSpace(existing.Address) ? sample.Address : existing.Address;
            }

            if (!await _userManager.IsInRoleAsync(existing, RoleConstants.Customer))
            {
                await _userManager.AddToRoleAsync(existing, RoleConstants.Customer);
            }

            users.Add(existing);
        }

        await _dbContext.SaveChangesAsync();
        return users;
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

    private async Task SeedSampleWalletAndTopUpsAsync(
        IReadOnlyList<ApplicationUser> customers,
        ApplicationUser? adminUser,
        ApplicationUser? operatorUser,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (customers.Count > 0)
        {
            var user = customers[0];
            var topUpTx = await EnsureWalletTransactionAsync(
                userId: user.Id,
                transactionType: WalletTransactionType.TopUpApproved,
                amount: 200000,
                balanceBefore: 0,
                balanceAfter: 200000,
                idempotencyKey: "seed-wallet-u1-topup-001",
                note: "Seed: Nạp ví đã duyệt",
                referenceType: "SeedTopUp",
                createdAt: now.AddDays(-6),
                performedByAdminId: adminUser?.Id,
                cancellationToken: cancellationToken);

            await EnsureWalletTransactionAsync(
                userId: user.Id,
                transactionType: WalletTransactionType.DebitForOrder,
                amount: -54000,
                balanceBefore: 200000,
                balanceAfter: 146000,
                idempotencyKey: "seed-wallet-u1-debit-001",
                note: "Seed: Trừ tiền đơn in",
                referenceType: "SeedPrintJob",
                createdAt: now.AddDays(-5),
                performedByAdminId: adminUser?.Id,
                cancellationToken: cancellationToken);

            await EnsureTopUpRequestAsync(new TopUpRequest
            {
                UserId = user.Id,
                Amount = 200000,
                TransferContent = "TOPUP SV01",
                TransactionReferenceCode = "SEED-VCB-0001",
                CreateIdempotencyKey = "seed-topup-u1-001",
                LastReviewIdempotencyKey = "seed-review-u1-001",
                Channel = TopUpChannel.BankTransfer,
                Status = TopUpStatus.Approved,
                RequiresAdminApproval = false,
                ReviewedByAdminId = operatorUser?.Id,
                ReviewedAt = now.AddDays(-6),
                ReviewNote = "Seed: Đã duyệt tự động",
                ApprovedWalletTransactionId = topUpTx.Id,
                CreatedAt = now.AddDays(-6)
            }, cancellationToken);

            user.CurrentBalance = 146000;
        }

        if (customers.Count > 1)
        {
            var user = customers[1];
            await EnsureWalletTransactionAsync(
                userId: user.Id,
                transactionType: WalletTransactionType.TopUpApproved,
                amount: 100000,
                balanceBefore: 0,
                balanceAfter: 100000,
                idempotencyKey: "seed-wallet-u2-topup-001",
                note: "Seed: Nạp ví thành công",
                referenceType: "SeedTopUp",
                createdAt: now.AddDays(-4),
                performedByAdminId: adminUser?.Id,
                cancellationToken: cancellationToken);

            await EnsureWalletTransactionAsync(
                userId: user.Id,
                transactionType: WalletTransactionType.DebitForOrder,
                amount: -26000,
                balanceBefore: 100000,
                balanceAfter: 74000,
                idempotencyKey: "seed-wallet-u2-debit-001",
                note: "Seed: Trừ tiền dịch vụ",
                referenceType: "SeedSupportOrder",
                createdAt: now.AddDays(-3),
                performedByAdminId: adminUser?.Id,
                cancellationToken: cancellationToken);

            await EnsureTopUpRequestAsync(new TopUpRequest
            {
                UserId = user.Id,
                Amount = 150000,
                TransferContent = "TOPUP SV02",
                TransactionReferenceCode = "SEED-VCB-0002",
                CreateIdempotencyKey = "seed-topup-u2-001",
                Channel = TopUpChannel.BankTransfer,
                Status = TopUpStatus.Pending,
                RequiresAdminApproval = false,
                CreatedAt = now.AddDays(-1)
            }, cancellationToken);

            user.CurrentBalance = 74000;
        }

        if (customers.Count > 2)
        {
            var user = customers[2];
            await EnsureWalletTransactionAsync(
                userId: user.Id,
                transactionType: WalletTransactionType.TopUpApproved,
                amount: 120000,
                balanceBefore: 0,
                balanceAfter: 120000,
                idempotencyKey: "seed-wallet-u3-topup-001",
                note: "Seed: Nạp ví thành công",
                referenceType: "SeedTopUp",
                createdAt: now.AddDays(-2),
                performedByAdminId: adminUser?.Id,
                cancellationToken: cancellationToken);

            await EnsureWalletTransactionAsync(
                userId: user.Id,
                transactionType: WalletTransactionType.DebitForOrder,
                amount: -18000,
                balanceBefore: 120000,
                balanceAfter: 102000,
                idempotencyKey: "seed-wallet-u3-debit-001",
                note: "Seed: Trừ tiền văn phòng phẩm",
                referenceType: "SeedProductOrder",
                createdAt: now.AddDays(-1),
                performedByAdminId: adminUser?.Id,
                cancellationToken: cancellationToken);

            await EnsureTopUpRequestAsync(new TopUpRequest
            {
                UserId = user.Id,
                Amount = 2500000,
                TransferContent = "TOPUP SV03 LON",
                TransactionReferenceCode = "SEED-VCB-0003",
                CreateIdempotencyKey = "seed-topup-u3-001",
                LastReviewIdempotencyKey = "seed-review-u3-001",
                Channel = TopUpChannel.BankTransfer,
                Status = TopUpStatus.PendingAdminApproval,
                RequiresAdminApproval = true,
                ReviewedByAdminId = operatorUser?.Id,
                ReviewedAt = now.AddHours(-12),
                ReviewNote = "Seed: Chờ admin duyệt bước 2",
                CreatedAt = now.AddDays(-1)
            }, cancellationToken);

            user.CurrentBalance = 102000;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSamplePrintJobsAsync(
        IReadOnlyList<ApplicationUser> customers,
        IReadOnlyDictionary<string, UploadedFileMetadata> uploadedFilesByUserId,
        ApplicationUser? adminUser,
        ApplicationUser? operatorUser,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var shippingFee = _configuration.GetValue<decimal?>("Business:ShippingFee") ?? 15000;

        if (customers.Count > 0 && uploadedFilesByUserId.TryGetValue(customers[0].Id, out var file1))
        {
            await EnsurePrintJobAsync(new PrintJob
            {
                UserId = customers[0].Id,
                UploadedFileId = file1.Id,
                PaperSize = PaperSize.A4,
                PrintSide = PrintSide.TwoSide,
                ColorMode = ColorMode.BlackWhite,
                IsPhoto = false,
                Copies = 2,
                TotalPages = 32,
                Notes = "In đóng tập cho môn Kinh tế vi mô.",
                DeliveryMethod = DeliveryMethod.PickupAtStore,
                UnitPrice = 1200,
                SubTotal = 76800,
                ShippingFee = 0,
                TotalAmount = 76800,
                Status = PrintJobStatus.Completed,
                ConfirmedByOperatorId = operatorUser?.Id,
                ConfirmedAt = now.AddDays(-5),
                AssignedOperatorId = operatorUser?.Id,
                LastStatusNote = "Đã in xong và khách đã nhận.",
                PaidAt = now.AddDays(-5),
                ProcessedByAdminId = adminUser?.Id,
                SubmitIdempotencyKey = "seed-print-u1-001",
                CreatedAt = now.AddDays(-5)
            }, cancellationToken);
        }

        if (customers.Count > 1 && uploadedFilesByUserId.TryGetValue(customers[1].Id, out var file2))
        {
            await EnsurePrintJobAsync(new PrintJob
            {
                UserId = customers[1].Id,
                UploadedFileId = file2.Id,
                PaperSize = PaperSize.A4,
                PrintSide = PrintSide.OneSide,
                ColorMode = ColorMode.Color,
                IsPhoto = false,
                Copies = 1,
                TotalPages = 24,
                Notes = "Trang 1 và trang kết luận cần màu sắc rõ.",
                DeliveryMethod = DeliveryMethod.Shipping,
                DeliveryAddress = "Quận 7, TP.HCM",
                UnitPrice = 2500,
                SubTotal = 60000,
                ShippingFee = shippingFee,
                TotalAmount = 60000 + shippingFee,
                Status = PrintJobStatus.Processing,
                ConfirmedByOperatorId = operatorUser?.Id,
                ConfirmedAt = now.AddDays(-1),
                AssignedOperatorId = operatorUser?.Id,
                LastStatusNote = "Đang in lô 2.",
                SubmitIdempotencyKey = "seed-print-u2-001",
                CreatedAt = now.AddDays(-1)
            }, cancellationToken);
        }

        if (customers.Count > 2 && uploadedFilesByUserId.TryGetValue(customers[2].Id, out var file3))
        {
            await EnsurePrintJobAsync(new PrintJob
            {
                UserId = customers[2].Id,
                UploadedFileId = file3.Id,
                PaperSize = PaperSize.A3,
                PrintSide = PrintSide.OneSide,
                ColorMode = ColorMode.BlackWhite,
                IsPhoto = false,
                Copies = 1,
                TotalPages = 10,
                Notes = "In khổ A3 dùng cho đồ án.",
                DeliveryMethod = DeliveryMethod.PickupAtStore,
                UnitPrice = 2000,
                SubTotal = 20000,
                ShippingFee = 0,
                TotalAmount = 20000,
                Status = PrintJobStatus.ConfirmedByShop,
                ConfirmedByOperatorId = operatorUser?.Id,
                ConfirmedAt = now.AddHours(-6),
                AssignedOperatorId = operatorUser?.Id,
                LastStatusNote = "Đã xác nhận file, chờ in.",
                SubmitIdempotencyKey = "seed-print-u3-001",
                CreatedAt = now.AddHours(-8)
            }, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSampleProductOrdersAsync(
        IReadOnlyList<ApplicationUser> customers,
        ApplicationUser? operatorUser,
        CancellationToken cancellationToken)
    {
        if (customers.Count == 0)
        {
            return;
        }

        var products = await _dbContext.Products
            .OrderBy(x => x.Name)
            .Take(3)
            .ToListAsync(cancellationToken);

        if (products.Count < 2)
        {
            return;
        }

        await EnsureProductOrderAsync(
            user: customers[0],
            orderIdempotencyKey: "seed-product-u1-001",
            deliveryMethod: DeliveryMethod.PickupAtStore,
            deliveryAddress: null,
            status: OrderStatus.Processing,
            processNote: "Đang chuẩn bị hàng.",
            processedByOperatorId: operatorUser?.Id,
            createdAt: DateTime.UtcNow.AddDays(-2),
            itemSeeds: new List<(Guid ProductId, int Quantity, decimal UnitPrice)>
            {
                (products[0].Id, 1, products[0].Price),
                (products[1].Id, 4, products[1].Price)
            },
            cancellationToken: cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSampleSupportOrdersAsync(
        IReadOnlyList<ApplicationUser> customers,
        ApplicationUser? operatorUser,
        CancellationToken cancellationToken)
    {
        if (customers.Count < 2)
        {
            return;
        }

        var service = await _dbContext.SupportServices
            .OrderBy(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            return;
        }

        var orderKey = "seed-support-u2-001";
        var exists = await _dbContext.SupportServiceOrders.AnyAsync(
            x => x.UserId == customers[1].Id && x.OrderIdempotencyKey == orderKey,
            cancellationToken);

        if (exists)
        {
            return;
        }

        var quantity = 2;
        var totalAmount = service.UnitPrice * quantity;

        _dbContext.SupportServiceOrders.Add(new SupportServiceOrder
        {
            UserId = customers[1].Id,
            SupportServiceId = service.Id,
            Quantity = quantity,
            UnitPrice = service.UnitPrice,
            TotalAmount = totalAmount,
            Notes = "Cần đóng gáy tài liệu nộp lớp.",
            OrderIdempotencyKey = orderKey,
            Status = OrderStatus.Submitted,
            ProcessedByOperatorId = operatorUser?.Id,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<WalletTransaction> EnsureWalletTransactionAsync(
        string userId,
        WalletTransactionType transactionType,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string idempotencyKey,
        string note,
        string referenceType,
        DateTime createdAt,
        string? performedByAdminId,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.WalletTransactions.FirstOrDefaultAsync(
            x => x.UserId == userId && x.TransactionType == transactionType && x.IdempotencyKey == idempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var transaction = new WalletTransaction
        {
            UserId = userId,
            TransactionType = transactionType,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            IdempotencyKey = idempotencyKey,
            Note = note,
            ReferenceType = referenceType,
            PerformedByAdminId = performedByAdminId,
            CreatedAt = createdAt
        };

        _dbContext.WalletTransactions.Add(transaction);
        return transaction;
    }

    private async Task EnsureTopUpRequestAsync(TopUpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CreateIdempotencyKey))
        {
            throw new InvalidOperationException("TopUpRequest seed phải có CreateIdempotencyKey để đảm bảo idempotent.");
        }

        var exists = await _dbContext.TopUpRequests.AnyAsync(
            x => x.UserId == request.UserId && x.CreateIdempotencyKey == request.CreateIdempotencyKey,
            cancellationToken);

        if (!exists)
        {
            _dbContext.TopUpRequests.Add(request);
        }
    }

    private async Task EnsurePrintJobAsync(PrintJob printJob, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(printJob.SubmitIdempotencyKey))
        {
            throw new InvalidOperationException("PrintJob seed phải có SubmitIdempotencyKey để đảm bảo idempotent.");
        }

        var exists = await _dbContext.PrintJobs.AnyAsync(
            x => x.UserId == printJob.UserId && x.SubmitIdempotencyKey == printJob.SubmitIdempotencyKey,
            cancellationToken);

        if (!exists)
        {
            _dbContext.PrintJobs.Add(printJob);
        }
    }

    private async Task EnsureProductOrderAsync(
        ApplicationUser user,
        string orderIdempotencyKey,
        DeliveryMethod deliveryMethod,
        string? deliveryAddress,
        OrderStatus status,
        string? processNote,
        string? processedByOperatorId,
        DateTime createdAt,
        IReadOnlyList<(Guid ProductId, int Quantity, decimal UnitPrice)> itemSeeds,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ProductOrders.AnyAsync(
            x => x.UserId == user.Id && x.OrderIdempotencyKey == orderIdempotencyKey,
            cancellationToken);

        if (exists)
        {
            return;
        }

        var order = new ProductOrder
        {
            UserId = user.Id,
            DeliveryMethod = deliveryMethod,
            DeliveryAddress = deliveryAddress,
            Notes = "Seed: Đơn văn phòng phẩm mẫu",
            OrderIdempotencyKey = orderIdempotencyKey,
            Status = status,
            ProcessedByOperatorId = processedByOperatorId,
            ProcessedAt = createdAt.AddHours(2),
            ProcessNote = processNote,
            CreatedAt = createdAt
        };

        foreach (var item in itemSeeds)
        {
            order.Items.Add(new ProductOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.UnitPrice * item.Quantity,
                CreatedAt = createdAt
            });
        }

        order.TotalAmount = order.Items.Sum(x => x.LineTotal);
        _dbContext.ProductOrders.Add(order);
    }
}
