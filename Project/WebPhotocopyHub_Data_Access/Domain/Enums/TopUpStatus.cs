using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum TopUpStatus
{
    [Display(Name = "Chờ duyệt")]
    Pending = 1,

    [Display(Name = "Đã duyệt")]
    Approved = 2,

    [Display(Name = "Từ chối")]
    Rejected = 3,

    [Display(Name = "Chờ admin duyệt bước 2")]
    PendingAdminApproval = 4
}
