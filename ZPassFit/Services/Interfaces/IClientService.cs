using ZPassFit.Dto;

namespace ZPassFit.Services.Interfaces;

public interface IClientService
{
    Task<ClientResponse?> GetMeAsync(string userId);
    Task<ClientResponse> UpsertMeAsync(string userId, UpsertClientMeRequest request);
    Task<ClientResponse?> GetByIdAsync(Guid id);
}