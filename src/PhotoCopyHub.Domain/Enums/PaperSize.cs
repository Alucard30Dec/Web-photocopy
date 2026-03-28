using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum PaperSize
{
    [Display(Name = "A5")]
    A5 = 1,

    [Display(Name = "A4")]
    A4 = 2,

    [Display(Name = "A3")]
    A3 = 3,

    [Display(Name = "A0")]
    A0 = 4
}
