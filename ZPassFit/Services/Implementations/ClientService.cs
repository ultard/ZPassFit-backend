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

    public async Task EnsureEntryLevelAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var existing = await clientLevelRepository.GetActiveByClientIdAsync(clientId);
        if (existing != null) return;

        var entryLevel = await levelRepository.GetEntryLevelAsync(cancellationToken);
        if (entryLevel == null)
            throw new InvalidOperationException("Entry loyalty level is not configured.");

        await clientLevelRepository.AddAsync(new ClientLevel
        {
            ClientId = clientId,
            LevelId = entryLevel.Id
        });
    }

    public async Task<bool> ApproveAsync(Guid clientId)
    {
        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null) return false;

        await EnsureEntryLevelAsync(clientId);
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

    public async Task<ClientResponse?> CreditBalanceAsync(Guid clientId, int amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Amount must be positive.");

        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null) return null;

        var sum = (long)client.Balance + amount;
        if (sum > int.MaxValue)
            throw new InvalidOperationException("Balance would exceed maximum allowed value.");

        client.Balance = (int)sum;
        await clientRepository.UpdateAsync(client);
        return Map(client);
    }

    public async Task<ClientResponse?> SetBalanceAsync(Guid clientId, int balance)
    {
        if (balance < 0)
            throw new InvalidOperationException("Balance cannot be negative.");

        var client = await clientRepository.GetByIdAsync(clientId);
        if (client == null) return null;

        client.Balance = balance;
        await clientRepository.UpdateAsync(client);
        return Map(client);
    }

    public async Task<ClientResponse?> UpdateMyProfileAsync(string userId, UpdateClientProfileRequest request)
    {
        var client = await clientRepository.GetByUserIdAsync(userId);
        if (client == null) return null;

        var birthDay = request.BirthDate.Date;
        if (birthDay > DateTime.UtcNow.Date)
            throw new InvalidOperationException("Birth date cannot be in the future.");

        client.LastName = request.LastName.Trim();
        client.FirstName = request.FirstName.Trim();
        client.MiddleName = request.MiddleName.Trim();
        client.BirthDate = DateTime.SpecifyKind(birthDay, DateTimeKind.Utc);
        client.Gender = request.Gender;

        await clientRepository.UpdateAsync(client);
        return Map(client);
    }

    private static ClientListItemResponse MapListItem(Client client)
    {
        return new ClientListItemResponse(
            client.Id,
            client.LastName,
            client.FirstName,
            client.MiddleName,
            client.Phone,
            client.Email,
            client.Status,
            client.RegistrationDate
        );
    }

    private static ClientResponse Map(Client client)
    {
        return new ClientResponse(
            client.Id,
            client.LastName,
            client.FirstName,
            client.MiddleName,
            client.BirthDate,
            client.Gender,
            client.Phone,
            client.Email,
            client.RegistrationDate,
            client.Status,
            client.Bonuses,
            client.Balance,
            client.Notes
        );
    }

    private static MyClientLevelResponse MapClientLevel(
        ClientLevel clientLevel,
        LevelResponse? nextLevel,
        int? remainingDaysToNextLevel)
    {
        return new MyClientLevelResponse(clientLevel.Id, clientLevel.ReceiveDate, MapLevel(clientLevel.Level),
            nextLevel, remainingDaysToNextLevel);
    }

    private static LevelResponse MapLevel(Level level)
    {
        return new LevelResponse(
            level.Id,
            level.Name,
            level.ActivateDays,
            level.GraceDays,
            level.PreviousLevelId,
            level.PreviousLevel?.Name
        );
    }
}