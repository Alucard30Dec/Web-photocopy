using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface IBranchContext
{
    Guid? BranchId { get; }
    string? BranchSlug { get; }
    string? BranchName { get; }
    bool IsActive { get; }
    bool IsAcceptingOrders { get; }
    bool EnforceBranchScope { get; }

    void Set(Branch branch, bool enforceBranchScope = true);
    void Clear();
}
