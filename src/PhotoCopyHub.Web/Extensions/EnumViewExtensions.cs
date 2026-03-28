using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Web.Extensions;

public static class EnumViewExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        if (member is null)
        {
            return value.ToString();
        }

        return member.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
    }

    public static string ToBadgeClass(this PrintJobStatus status) =>
        status switch
        {
            PrintJobStatus.Completed => "bg-success",
            PrintJobStatus.ReadyForPickup => "bg-primary",
            PrintJobStatus.Processing => "bg-warning text-dark",
            PrintJobStatus.ConfirmedByShop => "bg-info text-dark",
            PrintJobStatus.Paid => "bg-primary",
            PrintJobStatus.Cancelled => "bg-danger",
            PrintJobStatus.Refunded => "bg-danger",
            _ => "bg-secondary"
        };

    public static string ToBadgeClass(this OrderStatus status) =>
        status switch
        {
            OrderStatus.Completed => "bg-success",
            OrderStatus.Processing => "bg-warning text-dark",
            OrderStatus.Cancelled => "bg-danger",
            OrderStatus.Refunded => "bg-danger",
            _ => "bg-secondary"
        };

    public static string ToBadgeClass(this TopUpStatus status) =>
        status switch
        {
            TopUpStatus.Approved => "bg-success",
            TopUpStatus.Rejected => "bg-danger",
            TopUpStatus.PendingAdminApproval => "bg-info text-dark",
            _ => "bg-warning text-dark"
        };
}
