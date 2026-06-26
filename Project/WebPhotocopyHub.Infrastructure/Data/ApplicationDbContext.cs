using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Common;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IBranchContext? _branchContext;

    private bool BranchFilterEnabled => _branchContext?.EnforceBranchScope == true && _branchContext.BranchId.HasValue;
    private Guid CurrentBranchId => _branchContext?.BranchId ?? Guid.Empty;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IBranchContext? branchContext = null) : base(options)
    {
        _branchContext = branchContext;
    }

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
        // ChatGPT fix 2026-06-01: runtime đã chuẩn hóa PostgreSQL, không còn cấu hình MySQL GUID storage.

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.CurrentBalance).HasColumnType("decimal(18,2)");
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
                .HasFilter("\"Controller\" IS NOT NULL");
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

        builder.Entity<WalletTransaction>(entity =>
        {
            entity.HasQueryFilter(x => !BranchFilterEnabled || x.BranchId == CurrentBranchId);
            entity.HasIndex(x => new { x.BranchId, x.CreatedAt });
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.BalanceBefore).HasColumnType("decimal(18,2)");
            entity.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UserId).HasMaxLength(191);
            entity.Property(x => x.PerformedByAdminId).HasMaxLength(191);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.BranchId, x.UserId, x.TransactionType, x.IdempotencyKey }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.WalletTransactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PerformedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.PerformedByAdminId)
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

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IHasRowVersion>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
