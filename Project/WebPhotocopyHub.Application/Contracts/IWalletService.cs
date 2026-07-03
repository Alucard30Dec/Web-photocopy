using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface IWalletService
{
    Task<decimal> GetCurrentBalanceAsync(string userId, CancellationToken cancellationToken = default);
    Task<PagedResult<WalletTransaction>> GetUserTransactionsAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<PagedResult<WalletTransaction>> GetAllTransactionsAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<WalletTransaction> CreditAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default);
    Task<WalletTransaction> DebitAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default);
    Task<WalletTransaction> ManualAdjustAsync(WalletOperationRequestDto request, CancellationToken cancellationToken = default);
}
