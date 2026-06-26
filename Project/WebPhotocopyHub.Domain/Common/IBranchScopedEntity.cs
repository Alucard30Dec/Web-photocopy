namespace WebPhotocopyHub.Domain.Common;

public interface IBranchScopedEntity
{
    Guid BranchId { get; set; }
}
