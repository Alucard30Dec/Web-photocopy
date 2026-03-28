using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum DeliveryMethod
{
    [Display(Name = "Nhận tại tiệm")]
    PickupAtStore = 1,

    [Display(Name = "Giao tận nơi")]
    Shipping = 2
}
