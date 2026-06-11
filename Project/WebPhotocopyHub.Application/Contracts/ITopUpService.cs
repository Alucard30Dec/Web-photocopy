using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface ITopUpService
{
    Task<TopUpRequest> CreateRequestAsync(CreateTopUpRequestDto request, CancellationToken cancellationToken = default);
    Task<TopUpRequest> CreateCounterTopUpAsync(CreateCounterTopUpDto request, CancellationToken cancellationToken = default);
    Task<List<TopUpRequest>> GetUserRequestsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<TopUpRequest>> GetAllRequestsAsync(CancellationToken cancellationToken = default);
    Task<TopUpRequest> ReviewRequestAsync(ReviewTopUpRequestDto reviewDto, CancellationToken cancellationToken = default);
}
