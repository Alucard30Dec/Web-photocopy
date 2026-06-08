using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;

namespace PhotoCopyHub.Application.Contracts;

public interface IWalletService
{
    Task<decimal> GetCurrentBalanceAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<WalletTransaction>> GetUserTransactionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<WalletTransaction>> GetAllTransactionsAsync(CancellationToken cancellationToken = default);
    Task<WalletTransaction> CreditAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default);
    Task<WalletTransaction> DebitAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default);
    Task<WalletTransaction> ManualAdjustAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default);
}
