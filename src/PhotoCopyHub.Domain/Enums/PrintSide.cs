using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum PrintSide
{
    [Display(Name = "In 1 mặt")]
    OneSide = 1,

    [Display(Name = "In 2 mặt")]
    TwoSide = 2
}
