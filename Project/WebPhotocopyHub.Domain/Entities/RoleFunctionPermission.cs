namespace WebPhotocopyHub.Domain.Entities;

public class RoleFunctionPermission
{
    public string RoleId { get; set; } = string.Empty;
    public Guid SystemFunctionId { get; set; }

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }

    public ApplicationRoleProfile? RoleProfile { get; set; }
    public SystemFunction? SystemFunction { get; set; }
}
