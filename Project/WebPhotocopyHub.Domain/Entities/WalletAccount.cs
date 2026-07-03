using System.ComponentModel.DataAnnotations;
using WebPhotocopyHub.Domain.Common;

namespace WebPhotocopyHub.Domain.Entities;

public class WalletAccount : BaseEntity, IBranchScopedEntity
{
    public Guid BranchId { get; set; }

    [Required]
    [MaxLength(191)]
    public string UserId { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public long Version { get; set; } = 1;

    public ApplicationUser? User { get; set; }
    public Branch? Branch { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
