using WebPhotocopyHub.Application.DTOs;

namespace WebPhotocopyHub.Application.Contracts;

public interface IWalletReconciliationService
{
    Task<WalletBalanceReconciliationResultDto> ReconcileAsync(bool includeMatched, CancellationToken cancellationToken = default);
}
