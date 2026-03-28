using System.ComponentModel.DataAnnotations;
using PhotoCopyHub.Domain.Entities;

namespace PhotoCopyHub.Web.Models;

public class WalletIndexViewModel
{
    public decimal CurrentBalance { get; set; }
    public List<WalletTransaction> Transactions { get; set; } = new();
}

public class CreateTopUpRequestViewModel
{
    [Required(ErrorMessage = "Số tiền là bắt buộc")]
    [Range(1000, 100000000, ErrorMessage = "Số tiền phải từ 1.000 đến 100.000.000")]
    [Display(Name = "Số tiền nạp")]
    public decimal Amount { get; set; }

    [StringLength(200)]
    [Display(Name = "Nội dung chuyển khoản")]
    public string? TransferContent { get; set; }

    [StringLength(100)]
    [Display(Name = "Mã giao dịch")]
    public string? TransactionReferenceCode { get; set; }

    [Display(Name = "Ảnh minh chứng")]
    public IFormFile? ProofFile { get; set; }

    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}

public class TopUpPageViewModel
{
    public CreateTopUpRequestViewModel Form { get; set; } = new();
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string TransferContentPrefix { get; set; } = string.Empty;
}
