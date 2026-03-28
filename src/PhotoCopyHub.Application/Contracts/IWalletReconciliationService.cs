using PhotoCopyHub.Application.DTOs;

namespace PhotoCopyHub.Application.Contracts;

public interface IWalletReconciliationService
{
    Task<WalletBalanceReconciliationResultDto> ReconcileAsync(bool includeMatched, CancellationToken cancellationToken = default);
}
