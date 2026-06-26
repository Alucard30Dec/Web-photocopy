using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Infrastructure.Services;

public sealed class BranchContext : IBranchContext
{
    public Guid? BranchId { get; private set; }
    public string? BranchSlug { get; private set; }
    public string? BranchName { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsAcceptingOrders { get; private set; }
    public bool EnforceBranchScope { get; private set; }

    public void Set(Branch branch, bool enforceBranchScope = true)
    {
        BranchId = branch.Id;
        BranchSlug = branch.Slug;
        BranchName = branch.Name;
        IsActive = branch.IsActive;
        IsAcceptingOrders = branch.IsAcceptingOrders;
        EnforceBranchScope = enforceBranchScope;
    }

    public void Clear()
    {
        BranchId = null;
        BranchSlug = null;
        BranchName = null;
        IsActive = false;
        IsAcceptingOrders = false;
        EnforceBranchScope = false;
    }
}
