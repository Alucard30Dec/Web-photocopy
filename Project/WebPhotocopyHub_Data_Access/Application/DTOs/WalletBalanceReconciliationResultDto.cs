namespace PhotoCopyHub.Application.DTOs;

public class WalletBalanceReconciliationResultDto
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public int TotalUsers { get; set; }
    public int MatchedUsers { get; set; }
    public int MismatchUsers { get; set; }
    public List<WalletBalanceCheckItemDto> Items { get; set; } = new();
}
