namespace WebPhotocopyHub.Web.Admin.Authorization;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SystemPermissionActionAttribute : Attribute
{
    public SystemPermissionActionAttribute(string permissionAction)
    {
        PermissionAction = permissionAction;
    }

    public string PermissionAction { get; }
}
