using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Services.Implementations;

public class ClientService(
    IClientRepository clientRepository,
    IClientLevelRepository clientLevelRepository,
    ILevelRepository levelRepository,
    IVisitLogRepository visitLogRepository,
    IJwtTokenService jwtTokenService
) : IClientService
{
    public async Task<ClientResponse?> GetMeAsync(string userId)
    {
        var client = await clientRepository.GetByUserIdAsync(userId);
        return client == null ? null : Map(client);
    }

    public async Task<MyClientLevelResponse?> GetMyActiveLevelAsync(string userId)
    {
        var client = await clientRepository.GetByUserIdAsync(userId);
        if (client == null) return null;

        var clientLevel = await clientLevelRepository.GetActiveByClientIdAsync(client.Id);
        if (clientLevel == null) return null;

        var nextLevelEntity = await levelRepository.GetNextByPreviousLevelIdAsync(
            clientLevel.LevelId,
            CancellationToken.None);

        LevelResponse? nextLevelDto = null;
        int? remaining = null;

        if (nextLevelEntity == null) return MapClientLevel(clientLevel, nextLevelDto, remaining);

        nextLevelDto = MapLevel(nextLevelEntity);
        var visitDays = await visitLogRepository.CountDistinctVisitDaysByClientAsync(
            client.Id,
            client.RegistrationDate,
            CancellationToken.None);

        remaining = Math.Max(0, nextLevelEntity.ActivateDays - visitDays);

        return MapClientLevel(clientLevel, nextLevelDto, remaining);
    }

    public async Task<ClientResponse?> GetByIdAsync(Guid id)
    {
        var client = await clientRepository.GetByIdAsync(id);
        return client == null ? null : Map(client);
    }

    public async Task<bool> ApproveAsync(Guid clientId)
    {
        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null) return false;

        client.Status = ClientStatus.Active;
        await clientRepository.UpdateAsync(client);
        return true;
    }

    public async Task<bool> BlockAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null) return false;

        client.Status = ClientStatus.Blocked;
        await clientRepository.UpdateAsync(client);
        await jwtTokenService.RevokeAllRefreshTokensAsync(client.UserId, cancellationToken);
        return true;
    }

    public async Task<bool> UnblockAsync(Guid clientId)
    {
        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null) return false;

        client.Status = ClientStatus.Active;
        await clientRepository.UpdateAsync(client);
        return true;
    }

    public async Task<PagedClientsResponse> SearchPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        var (items, total) = await clientRepository.SearchPagedAsync(
            search,
            (page - 1) * pageSize,
            pageSize,
            cancellationToken
        );

        var mapped = items.Select(MapListItem).ToList();
        return new PagedClientsResponse(page, pageSize, total, mapped);
    }

    private static ClientListItemResponse MapListItem(Client c)
    {
        return new ClientListItemResponse(
            c.Id,
            c.LastName,
            c.FirstName,
            c.MiddleName,
            c.Phone,
            c.Email,
            c.Status,
            c.RegistrationDate
        );
    }

    private static ClientResponse Map(Client c)
    {
        return new ClientResponse(
            c.Id,
            c.LastName,
            c.FirstName,
            c.MiddleName,
            c.BirthDate,
            c.Gender,
            c.Phone,
            c.Email,
            c.RegistrationDate,
            c.Status,
            c.Bonuses,
            c.Notes
        );
    }

    private static MyClientLevelResponse MapClientLevel(
        ClientLevel cl,
        LevelResponse? nextLevel,
        int? remainingDaysToNextLevel)
    {
        return new MyClientLevelResponse(cl.Id, cl.ReceiveDate, MapLevel(cl.Level), nextLevel, remainingDaysToNextLevel);
    }

    private static LevelResponse MapLevel(Level l)
    {
        return new LevelResponse(
            l.Id,
            l.Name,
            l.ActivateDays,
            l.GraceDays,
            l.PreviousLevelId,
            l.PreviousLevel?.Name
        );
    }
}