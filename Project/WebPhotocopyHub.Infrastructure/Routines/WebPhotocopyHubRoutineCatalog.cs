namespace WebPhotocopyHub.DataAccess.Routines;

public interface IWebPhotocopyHubRoutineCatalog
{
    bool IsAllowed(string p_strRoutineName);
    IReadOnlyCollection<string> ListRoutineNames();
}

public sealed class WebPhotocopyHubRoutineCatalog : IWebPhotocopyHubRoutineCatalog
{
    public const string GetBranchWalletBalance = "operations.get_branch_wallet_balance";
    public const string ReconcileBranchWallet = "operations.reconcile_branch_wallet";
    public const string LaySoDuViChiNhanh = "vi.lay_so_du_vi_chi_nhanh";
    public const string DoiSoatViChiNhanh = "vi.doi_soat_vi_chi_nhanh";

    private static readonly HashSet<string> Arr_Routine_Name = new(StringComparer.Ordinal)
    {
        GetBranchWalletBalance,
        ReconcileBranchWallet,
        LaySoDuViChiNhanh,
        DoiSoatViChiNhanh
    };

    public bool IsAllowed(string p_strRoutineName)
    {
        return Arr_Routine_Name.Contains(p_strRoutineName);
    }

    public IReadOnlyCollection<string> ListRoutineNames()
    {
        return Arr_Routine_Name.OrderBy(p_strName => p_strName, StringComparer.Ordinal).ToList();
    }
}
