using System.Text;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Report;

public interface IAdminCsvReportService
{
    byte[] BuildTopUpRequestsCsv(IEnumerable<TopUpRequest> p_arrData);
    byte[] BuildWalletTransactionsCsv(IEnumerable<WalletTransaction> p_arrData);
}

public static class ReportNameCatalog
{
    public const string rpt9001_A_M01_Admin_Top_Up_Request_Csv = "rpt9001_A_M01_Admin_Top_Up_Request_Csv";
    public const string rpt9001_A_M02_Admin_Wallet_Transaction_Csv = "rpt9001_A_M02_Admin_Wallet_Transaction_Csv";
}

public sealed class AdminCsvReportService : IAdminCsvReportService
{
    public byte[] BuildTopUpRequestsCsv(IEnumerable<TopUpRequest> p_arrData)
    {
        var v_sbData = new StringBuilder();
        v_sbData.AppendLine("Id,CreatedAtUtc,UserEmail,UserId,Amount,Channel,Status,TransferContent,TransactionReferenceCode,RequiresAdminApproval,ReviewedBy,SecondReviewedBy,ReviewNote,SecondReviewNote");

        foreach (var v_objItem in p_arrData)
        {
            v_sbData.AppendLine(string.Join(",",
                EscapeCsv(v_objItem.Id.ToString()),
                EscapeCsv(v_objItem.CreatedAt.ToString("O")),
                EscapeCsv(v_objItem.User?.Email),
                EscapeCsv(v_objItem.UserId),
                EscapeCsv(v_objItem.Amount.ToString()),
                EscapeCsv(v_objItem.Channel.ToString()),
                EscapeCsv(v_objItem.Status.ToString()),
                EscapeCsv(v_objItem.TransferContent),
                EscapeCsv(v_objItem.TransactionReferenceCode),
                EscapeCsv(v_objItem.RequiresAdminApproval.ToString()),
                EscapeCsv(v_objItem.ReviewedByAdminId),
                EscapeCsv(v_objItem.SecondReviewedByAdminId),
                EscapeCsv(v_objItem.ReviewNote),
                EscapeCsv(v_objItem.SecondReviewNote)));
        }

        return Encoding.UTF8.GetBytes(v_sbData.ToString());
    }

    public byte[] BuildWalletTransactionsCsv(IEnumerable<WalletTransaction> p_arrData)
    {
        var v_sbData = new StringBuilder();
        v_sbData.AppendLine("Id,CreatedAtUtc,UserId,TransactionType,Amount,BalanceBefore,BalanceAfter,ReferenceType,ReferenceId,IdempotencyKey,PerformedByAdminId,Note");

        foreach (var v_objItem in p_arrData)
        {
            v_sbData.AppendLine(string.Join(",",
                EscapeCsv(v_objItem.Id.ToString()),
                EscapeCsv(v_objItem.CreatedAt.ToString("O")),
                EscapeCsv(v_objItem.UserId),
                EscapeCsv(v_objItem.TransactionType.ToString()),
                EscapeCsv(v_objItem.Amount.ToString()),
                EscapeCsv(v_objItem.BalanceBefore.ToString()),
                EscapeCsv(v_objItem.BalanceAfter.ToString()),
                EscapeCsv(v_objItem.ReferenceType),
                EscapeCsv(v_objItem.ReferenceId?.ToString()),
                EscapeCsv(v_objItem.IdempotencyKey),
                EscapeCsv(v_objItem.PerformedByAdminId),
                EscapeCsv(v_objItem.Note)));
        }

        return Encoding.UTF8.GetBytes(v_sbData.ToString());
    }

    private static string EscapeCsv(string? p_strValue)
    {
        if (string.IsNullOrEmpty(p_strValue))
        {
            return "\"\"";
        }

        var v_strEscaped = p_strValue.Replace("\"", "\"\"");
        return $"\"{v_strEscaped}\"";
    }
}
