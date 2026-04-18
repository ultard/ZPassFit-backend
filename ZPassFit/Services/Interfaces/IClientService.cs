using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IClientService
{
    Task<ClientResponse?> GetMeAsync(string userId);
    Task<MyClientLevelResponse?> GetMyActiveLevelAsync(string userId);
    Task<ClientResponse?> GetByIdAsync(Guid id);
    Task<bool> ApproveAsync(Guid clientId);
    Task<bool> BlockAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<bool> UnblockAsync(Guid clientId);

    Task<PagedClientsResponse> SearchPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );
}