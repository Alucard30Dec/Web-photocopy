namespace WebPhotocopyHub.Application.Security;

public static class SystemPermissionActions
{
    public const string View = "View";
    public const string Create = "Create";
    public const string Edit = "Edit";
    public const string Delete = "Delete";
    public const string Export = "Export";

    public static readonly IReadOnlyList<string> All = new[]
    {
        View,
        Create,
        Edit,
        Delete,
        Export
    };
}
