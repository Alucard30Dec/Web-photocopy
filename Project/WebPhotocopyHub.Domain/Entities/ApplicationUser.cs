using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using WebPhotocopyHub.Domain.Common;

namespace WebPhotocopyHub.Domain.Entities;

public class ApplicationUser : IdentityUser, IHasRowVersion
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }

    [Obsolete("Legacy balance kept for migration/rollback only. Use WalletAccount per branch as the balance source.")]
    public decimal CurrentBalance { get; set; }

    [NotMapped]
    public decimal BranchWalletBalance { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
    public ICollection<TopUpRequest> TopUpRequests { get; set; } = new List<TopUpRequest>();
    public ICollection<PrintJob> PrintJobs { get; set; } = new List<PrintJob>();
    public ICollection<ProductOrder> ProductOrders { get; set; } = new List<ProductOrder>();
    public ICollection<SupportServiceOrder> SupportServiceOrders { get; set; } = new List<SupportServiceOrder>();
    public ICollection<ProductStockMovement> ProductStockMovements { get; set; } = new List<ProductStockMovement>();
    public ICollection<UploadedFileMetadata> UploadedFiles { get; set; } = new List<UploadedFileMetadata>();
    public ICollection<UserBranchMembership> BranchMemberships { get; set; } = new List<UserBranchMembership>();
}
