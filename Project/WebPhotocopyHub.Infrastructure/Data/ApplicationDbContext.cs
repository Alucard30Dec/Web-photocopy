using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Common;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private const string AppSchema = "app";
    private const string AuditSchema = "audit";
    private const string SystemSchema = "system";
    private const string PublicSchema = "public";

    private static readonly Dictionary<Type, (string Schema, string Table)> BusinessTableMappings = new()
    {
        [typeof(Branch)] = (AppSchema, "shop_branches"),
        [typeof(BranchFeature)] = (AppSchema, "branch_features"),
        [typeof(BranchRole)] = (AppSchema, "branch_roles"),
        [typeof(BranchRolePermission)] = (AppSchema, "branch_role_permissions"),
        [typeof(UserBranchMembership)] = (AppSchema, "user_branch_memberships"),
        [typeof(WalletAccount)] = (AppSchema, "branch_wallets"),
        [typeof(WalletTransaction)] = (AppSchema, "wallet_transactions"),
        [typeof(TopUpRequest)] = (AppSchema, "top_up_requests"),
        [typeof(UploadedFileMetadata)] = (AppSchema, "uploaded_files"),
        [typeof(PrintJob)] = (AppSchema, "print_jobs"),
        [typeof(Product)] = (AppSchema, "products"),
        [typeof(ProductOrder)] = (AppSchema, "product_orders"),
        [typeof(ProductOrderItem)] = (AppSchema, "product_order_items"),
        [typeof(ProductStockMovement)] = (AppSchema, "product_stock_movements"),
        [typeof(SupportService)] = (AppSchema, "support_services"),
        [typeof(SupportServiceOrder)] = (AppSchema, "support_service_orders"),
        [typeof(PricingRule)] = (AppSchema, "pricing_rules"),
        [typeof(AuditLog)] = (AuditSchema, "audit_logs"),
        [typeof(SystemFunction)] = (SystemSchema, "system_functions"),
        [typeof(ApplicationRoleProfile)] = (SystemSchema, "application_role_profiles"),
        [typeof(RoleFunctionPermission)] = (SystemSchema, "role_function_permissions")
    };

    private readonly IBranchContext? _branchContext;

    private bool BranchFilterEnabled => _branchContext?.EnforceBranchScope == true && _branchContext.BranchId.HasValue;
    private Guid CurrentBranchId => _branchContext?.BranchId ?? Guid.Empty;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IBranchContext? branchContext = null) : base(options)
    {
        _branchContext = branchContext;
    }

    public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<TopUpRequest> TopUpRequests => Set<TopUpRequest>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductOrder> ProductOrders => Set<ProductOrder>();
    public DbSet<ProductOrderItem> ProductOrderItems => Set<ProductOrderItem>();
    public DbSet<ProductStockMovement> ProductStockMovements => Set<ProductStockMovement>();
    public DbSet<SupportService> SupportServices => Set<SupportService>();
    public DbSet<SupportServiceOrder> SupportServiceOrders => Set<SupportServiceOrder>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();
    public DbSet<UploadedFileMetadata> UploadedFileMetadatas => Set<UploadedFileMetadata>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BranchFeature> BranchFeatures => Set<BranchFeature>();
    public DbSet<BranchRole> BranchRoles => Set<BranchRole>();
    public DbSet<BranchRolePermission> BranchRolePermissions => Set<BranchRolePermission>();
    public DbSet<UserBranchMembership> UserBranchMemberships => Set<UserBranchMembership>();
    public DbSet<SystemFunction> SystemFunctions => Set<SystemFunction>();
    public DbSet<ApplicationRoleProfile> ApplicationRoleProfiles => Set<ApplicationRoleProfile>();
    public DbSet<RoleFunctionPermission> RoleFunctionPermissions => Set<RoleFunctionPermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureIdentityKeyLengths(builder);
        ConfigureIdentityPhysicalNames(builder);
        ConfigureBusinessPhysicalNames(builder);
        // ChatGPT fix 2026-06-01: runtime đã chuẩn hóa PostgreSQL, không còn cấu hình MySQL GUID storage.

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property<decimal>("CurrentBalance").HasColumnType("decimal(18,2)");
            entity.Property(x => x.RowVersion).IsConcurrencyToken();
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.HasIndex(x => x.CreatedAt);
        });

        builder.Entity<Branch>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Slug).HasMaxLength(80);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.RowVersion).IsConcurrencyToken();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.Name });
        });

        builder.Entity<BranchFeature>(entity =>
        {
            entity.HasKey(x => new { x.BranchId, x.FeatureCode });
            entity.Property(x => x.FeatureCode).HasMaxLength(100);
            entity.Property(x => x.UpdatedByUserId).HasMaxLength(191);
            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Features)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BranchRole>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasIndex(x => new { x.BranchId, x.Name }).IsUnique();
            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BranchRolePermission>(entity =>
        {
            entity.HasKey(x => new { x.BranchRoleId, x.PermissionCode });
            entity.Property(x => x.PermissionCode).HasMaxLength(120);
            entity.HasOne(x => x.BranchRole)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.BranchRoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserBranchMembership>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(191);
            entity.Property(x => x.AssignedByUserId).HasMaxLength(191);
            entity.HasIndex(x => new { x.UserId, x.BranchId }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.BranchMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.BranchRole)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.BranchRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SystemFunction>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Area).HasMaxLength(50);
            entity.Property(x => x.Controller).HasMaxLength(100);
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.Property(x => x.IconKey).HasMaxLength(50);
            entity.Property(x => x.RequiredBranchFeatureCode).HasMaxLength(100);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.Area, x.Controller })
                .IsUnique()
                .HasFilter("controller IS NOT NULL");
            entity.HasIndex(x => new { x.ParentId, x.SortOrder });
            entity.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationRoleProfile>(entity =>
        {
            entity.Property(x => x.RoleId).HasMaxLength(191);
            entity.Property(x => x.DisplayName).HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasOne(x => x.Role)
                .WithOne()
                .HasForeignKey<ApplicationRoleProfile>(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RoleFunctionPermission>(entity =>
        {
            entity.HasKey(x => new { x.RoleId, x.SystemFunctionId });
            entity.Property(x => x.RoleId).HasMaxLength(191);
            entity.HasOne(x => x.RoleProfile)
                .WithMany(x => x.FunctionPermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SystemFunction)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.SystemFunctionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WalletAccount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(191);
            entity.Property(x => x.Balance).HasColumnType("numeric(18,2)");
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.UserId, x.BranchId }).IsUnique();
            entity.HasIndex(x => new { x.BranchId, x.UserId });
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WalletTransaction>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.CreatedAt });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.BalanceBefore).HasColumnType("decimal(18,2)");
            entity.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UserId).HasMaxLength(191);
            entity.Property(x => x.WalletAccountId).HasColumnName("branch_wallet_id");
            entity.Property(x => x.PerformedByAdminId).HasMaxLength(191);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.BranchId, x.UserId, x.TransactionType, x.IdempotencyKey })
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(x => new { x.WalletAccountId, x.CreatedAt });

            entity.HasOne(x => x.User)
                .WithMany(x => x.WalletTransactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PerformedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.PerformedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WalletAccount)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.WalletAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopUpRequest>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.CreatedAt });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UserId).HasMaxLength(191);
            entity.Property(x => x.ReviewedByAdminId).HasMaxLength(191);
            entity.Property(x => x.SecondReviewedByAdminId).HasMaxLength(191);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.BranchId, x.UserId, x.CreateIdempotencyKey }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.TopUpRequests)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SecondReviewedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.SecondReviewedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ProofFile)
                .WithMany()
                .HasForeignKey(x => x.ProofFileId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<UploadedFileMetadata>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.CreatedAt });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.OwnerUserId).HasMaxLength(191);

            entity.HasOne(x => x.OwnerUser)
                .WithMany(x => x.UploadedFiles)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.OwnerUserId, x.CreatedAt });
        });

        builder.Entity<PrintJob>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.Status, x.CreatedAt });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ShippingFee).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UserId).HasMaxLength(191);
            entity.Property(x => x.ConfirmedByOperatorId).HasMaxLength(191);
            entity.Property(x => x.AssignedOperatorId).HasMaxLength(191);
            entity.Property(x => x.ProcessedByAdminId).HasMaxLength(191);
            entity.Property(x => x.RefundedByUserId).HasMaxLength(191);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.BranchId, x.UserId, x.SubmitIdempotencyKey }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.PrintJobs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ProcessedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ProcessedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ConfirmedByOperator)
                .WithMany()
                .HasForeignKey(x => x.ConfirmedByOperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedOperator)
                .WithMany()
                .HasForeignKey(x => x.AssignedOperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.RefundedByUser)
                .WithMany()
                .HasForeignKey(x => x.RefundedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UploadedFile)
                .WithMany(x => x.PrintJobs)
                .HasForeignKey(x => x.UploadedFileId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Product>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.IsActive, x.Name });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
            entity.Property(x => x.RowVersion).IsConcurrencyToken();
            entity.HasIndex(x => x.IsActive);
        });

        builder.Entity<ProductOrder>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.Status, x.CreatedAt });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UserId).HasMaxLength(191);
            entity.Property(x => x.ProcessedByOperatorId).HasMaxLength(191);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.BranchId, x.UserId, x.OrderIdempotencyKey }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.ProductOrders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ProcessedByOperator)
                .WithMany()
                .HasForeignKey(x => x.ProcessedByOperatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProductOrderItem>(entity =>
        {
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.ProductOrder)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.ProductOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.ProductOrderItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SupportService>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.IsActive, x.Name });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => x.IsActive);
        });

        builder.Entity<SupportServiceOrder>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.Status, x.CreatedAt });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UserId).HasMaxLength(191);
            entity.Property(x => x.ProcessedByOperatorId).HasMaxLength(191);
            entity.HasIndex(x => new { x.BranchId, x.UserId, x.OrderIdempotencyKey }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.SupportServiceOrders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ProcessedByOperator)
                .WithMany()
                .HasForeignKey(x => x.ProcessedByOperatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SupportService)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.SupportServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProductStockMovement>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.CreatedAt });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.ActorUserId).HasMaxLength(191);
            entity.HasIndex(x => new { x.ProductId, x.CreatedAt });

            entity.HasOne(x => x.Product)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ActorUser)
                .WithMany(x => x.ProductStockMovements)
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PricingRule>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(x => new { x.BranchId, x.PaperSize, x.PrintSide, x.ColorMode, x.IsPhoto }).IsUnique();
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.RecordHash).IsUnique();
        });

        ConfigureTrackingColumns(builder);
        ConfigureBusinessColumnNames(builder);
    }

    private static void ConfigureIdentityPhysicalNames(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers", PublicSchema);
        builder.Entity<IdentityRole>().ToTable("AspNetRoles", PublicSchema);
        builder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims", PublicSchema);
        builder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins", PublicSchema);
        builder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles", PublicSchema);
        builder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens", PublicSchema);
        builder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims", PublicSchema);
    }

    private static void ConfigureBusinessPhysicalNames(ModelBuilder builder)
    {
        foreach (var mapping in BusinessTableMappings)
        {
            builder.Entity(mapping.Key).ToTable(mapping.Value.Table, mapping.Value.Schema);
        }
    }

    private static void ConfigureTrackingColumns(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(x => BusinessTableMappings.ContainsKey(x.ClrType)
                         && typeof(BaseEntity).IsAssignableFrom(x.ClrType)))
        {
            var entity = builder.Entity(entityType.ClrType);
            entity.Property<int>(nameof(BaseEntity.IsDeleted))
                .HasColumnName("is_deleted")
                .HasDefaultValue(0);
            entity.Property<DateTime>(nameof(BaseEntity.CreatedAt))
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property<string?>(nameof(BaseEntity.CreatedBy))
                .HasColumnName("created_by")
                .HasMaxLength(100);
            entity.Property<string?>(nameof(BaseEntity.CreatedByFunction))
                .HasColumnName("created_by_function")
                .HasMaxLength(100);
            entity.Property<DateTime?>(nameof(BaseEntity.UpdatedAt))
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property<string?>(nameof(BaseEntity.UpdatedBy))
                .HasColumnName("updated_by")
                .HasMaxLength(100);
            entity.Property<string?>(nameof(BaseEntity.UpdatedByFunction))
                .HasColumnName("updated_by_function")
                .HasMaxLength(100);
            var mapping = BusinessTableMappings[entityType.ClrType];
            entity.ToTable(mapping.Table, mapping.Schema, tableBuilder =>
                tableBuilder.HasCheckConstraint(
                    $"ck_{mapping.Table}_is_deleted",
                    "is_deleted IN (0, 1)"));
        }
    }

    private static void ConfigureBusinessColumnNames(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(x => BusinessTableMappings.ContainsKey(x.ClrType)))
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(GetBusinessColumnName(entityType.ClrType, property.Name));
            }
        }
    }

    private static string GetBusinessColumnName(Type entityType, string propertyName)
    {
        if (entityType == typeof(WalletTransaction) && propertyName == nameof(WalletTransaction.WalletAccountId))
        {
            return "branch_wallet_id";
        }

        return ToSnakeCase(propertyName);
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current))
            {
                var previousIsLowerOrDigit = i > 0 && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1]));
                var nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);
                if (builder.Length > 0 && (previousIsLowerOrDigit || nextIsLower))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static void ConfigureIdentityKeyLengths(ModelBuilder builder)
    {
        const int keyLength = 191;

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.Id).HasMaxLength(keyLength);
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.Property(x => x.Id).HasMaxLength(keyLength);
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(keyLength);
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(keyLength);
            entity.Property(x => x.LoginProvider).HasMaxLength(keyLength);
            entity.Property(x => x.ProviderKey).HasMaxLength(keyLength);
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(keyLength);
            entity.Property(x => x.RoleId).HasMaxLength(keyLength);
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(keyLength);
            entity.Property(x => x.LoginProvider).HasMaxLength(keyLength);
            entity.Property(x => x.Name).HasMaxLength(keyLength);
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.Property(x => x.RoleId).HasMaxLength(keyLength);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditableRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditableRules();
        return base.SaveChanges();
    }

    private void ApplyAuditableRules()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IBranchScopedEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.BranchId == Guid.Empty)
            {
                entry.Entity.BranchId = _branchContext?.BranchId ?? BranchDefaults.PrimaryBranchId;
            }
        }

        ApplyTrackingRules(now);

        foreach (var entry in ChangeTracker.Entries<IHasRowVersion>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }

    private void ApplyTrackingRules(DateTime now)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy))
                {
                    entry.Entity.CreatedBy = "application";
                }

                if (string.IsNullOrWhiteSpace(entry.Entity.CreatedByFunction))
                {
                    entry.Entity.CreatedByFunction = "ApplicationDbContext.SaveChanges";
                }
            }
            else
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedBy).IsModified = false;
                entry.Property(x => x.CreatedByFunction).IsModified = false;
            }

            entry.Entity.UpdatedAt = now;
            if (string.IsNullOrWhiteSpace(entry.Entity.UpdatedBy))
            {
                entry.Entity.UpdatedBy = "application";
            }

            if (string.IsNullOrWhiteSpace(entry.Entity.UpdatedByFunction))
            {
                entry.Entity.UpdatedByFunction = "ApplicationDbContext.SaveChanges";
            }

            if (entry.Entity.IsDeleted is not (0 or 1))
            {
                throw new InvalidOperationException("BaseEntity.IsDeleted chỉ được nhận giá trị 0 hoặc 1.");
            }

            if (entry.State == EntityState.Modified)
            {
                if (entry.Property(x => x.IsDeleted).OriginalValue == 1 &&
                    entry.Property(x => x.IsDeleted).CurrentValue == 0)
                {
                    throw new InvalidOperationException("Không được khôi phục bản ghi soft-delete nếu chưa có nghiệp vụ riêng.");
                }
            }
        }
    }
}
