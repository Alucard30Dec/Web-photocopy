using System.ComponentModel.DataAnnotations;

namespace PhotoCopyHub.Domain.Enums;

public enum ColorMode
{
    [Display(Name = "Trắng đen")]
    BlackWhite = 1,

    [Display(Name = "In màu")]
    Color = 2
}
