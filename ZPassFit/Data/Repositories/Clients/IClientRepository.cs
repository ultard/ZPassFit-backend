using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories;

namespace ZPassFit.Data.Repositories.Clients;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id);
    Task<Client?> GetByUserIdAsync(string userId);
    Task AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(Guid id);
    Task<int> CountAsync();

    Task<int> CountRegisteredBetweenAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<ClubDayCountRow>> GetRegistrationCountsByClubDayAsync(
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        string timeZoneId,
        CancellationToken cancellationToken = default
    );

    Task<(IReadOnlyList<Client> Items, int TotalCount)> SearchPagedAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default
    );
}