namespace PhotoCopyHub.Application.DTOs;

public class WalletBalanceCheckItemDto
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal LedgerBalance { get; set; }
    public decimal Difference { get; set; }
    public bool IsMatched => Difference == 0;
}
