using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class ClientServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task GetMe_Missing_ReturnsNull(
        [Frozen] IClientRepository clientRepository,
        ClientService clientService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepository);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Client?)null);
        var result = await clientService.GetMeAsync(userId);

        Assert.Null(result);
        clientRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task UpsertMe_Missing_AddsAndMaps(
        [Frozen] IClientRepository clientRepository,
        ClientService clientService
    )
    {
        // Arrange
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepository);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Client?)null);

        Client? added = null;
        clientRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Client>()))
            .Callback<Client>(addedClient => added = addedClient)
            .Returns(Task.CompletedTask);

        var request = new UpsertClientMeRequest(
            "Ivanov",
            "Ivan",
            "Ivanovich",
            new DateTime(2000, 1, 2),
            ClientGender.Male,
            "+70000000000",
            "ivan@example.com",
            "note"
        );

        var before = DateTime.UtcNow;

        // Act
        var result = await clientService.UpsertMeAsync(userId, request);
        var after = DateTime.UtcNow;

        // Assert
        Assert.NotNull(added);
        Assert.Equal(userId, added!.UserId);
        Assert.Equal(request.LastName, added.LastName);
        Assert.Equal(request.FirstName, added.FirstName);
        Assert.Equal(request.MiddleName, added.MiddleName);
        Assert.Equal(request.BirthDate, added.BirthDate);
        Assert.Equal(request.Gender, added.Gender);
        Assert.Equal(request.Phone, added.Phone);
        Assert.Equal(request.Email, added.Email);
        Assert.Equal(request.Notes, added.Notes);

        Assert.Equal(added.Id, result.Id);
        Assert.Equal(request.LastName, result.LastName);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.MiddleName, result.MiddleName);
        Assert.Equal(request.BirthDate, result.BirthDate);
        Assert.Equal(request.Gender, result.Gender);
        Assert.Equal(request.Phone, result.Phone);
        Assert.Equal(request.Email, result.Email);
        Assert.True(result.RegistrationDate >= before.AddSeconds(-5) && result.RegistrationDate <= after.AddSeconds(5));

        clientRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task UpsertMe_Existing_UpdatesAndMaps(
        [Frozen] IClientRepository clientRepository,
        ClientService clientService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepository);
        var existingClient = new Client
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LastName = "Petrov",
            FirstName = "Petr",
            MiddleName = "Petrovich",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "+79990000000",
            Email = "petrov.old@example.com",
            Notes = null
        };

        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existingClient);
        clientRepositoryMock.Setup(r => r.UpdateAsync(existingClient)).Returns(Task.CompletedTask);

        var request = new UpsertClientMeRequest(
            "Ivanov",
            "Ivan",
            "Ivanovich",
            new DateTime(2000, 1, 2),
            ClientGender.Male,
            "+70000000000",
            "ivan@example.com",
            "note"
        );

        var result = await clientService.UpsertMeAsync(userId, request);

        Assert.Equal(existingClient.Id, result.Id);
        Assert.Equal(request.LastName, existingClient.LastName);
        Assert.Equal(request.FirstName, existingClient.FirstName);
        Assert.Equal(request.MiddleName, existingClient.MiddleName);
        Assert.Equal(request.BirthDate, existingClient.BirthDate);
        Assert.Equal(request.Gender, existingClient.Gender);
        Assert.Equal(request.Phone, existingClient.Phone);
        Assert.Equal(request.Email, existingClient.Email);
        Assert.Equal(request.Notes, existingClient.Notes);

        clientRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetById_Missing_ReturnsNull(
        [Frozen] IClientRepository clientRepository,
        ClientService clientService
    )
    {
        var clientId = Guid.NewGuid();
        var clientRepositoryMock = Mock.Get(clientRepository);
        clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync((Client?)null);

        var result = await clientService.GetByIdAsync(clientId);

        Assert.Null(result);
        clientRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetMyActiveLevel_NoClient_ReturnsNull(
        [Frozen] IClientRepository clientRepository,
        [Frozen] IClientLevelRepository clientLevelRepository,
        ClientService clientService
    )
    {
        var userId = "u1";
        var clientRepositoryMock = Mock.Get(clientRepository);
        var clientLevelRepositoryMock = Mock.Get(clientLevelRepository);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Client?)null);

        var result = await clientService.GetMyActiveLevelAsync(userId);

        Assert.Null(result);
        clientRepositoryMock.VerifyAll();
        clientLevelRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetMyActiveLevel_NoActiveLevel_ReturnsNull(
        [Frozen] IClientRepository clientRepository,
        [Frozen] IClientLevelRepository clientLevelRepository,
        ClientService clientService
    )
    {
        var userId = "u1";
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LastName = "Petrov",
            FirstName = "Petr",
            MiddleName = "P",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "+79990000000",
            Email = "petrov@example.com"
        };

        var clientRepositoryMock = Mock.Get(clientRepository);
        var clientLevelRepositoryMock = Mock.Get(clientLevelRepository);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);
        clientLevelRepositoryMock.Setup(r => r.GetActiveByClientIdAsync(client.Id)).ReturnsAsync((ClientLevel?)null);

        var result = await clientService.GetMyActiveLevelAsync(userId);

        Assert.Null(result);
        clientRepositoryMock.VerifyAll();
        clientLevelRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetMyActiveLevel_WithLevel_Maps(
        [Frozen] IClientRepository clientRepository,
        [Frozen] IClientLevelRepository clientLevelRepository,
        ClientService clientService
    )
    {
        var userId = "u1";
        var levelId = Guid.NewGuid();
        var prevId = Guid.NewGuid();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LastName = "Petrov",
            FirstName = "Petr",
            MiddleName = "P",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "+79990000000",
            Email = "petrov@example.com"
        };

        var clientLevel = new ClientLevel
        {
            Id = 42,
            ClientId = client.Id,
            LevelId = levelId,
            ReceiveDate = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            Level = new Level
            {
                Id = levelId,
                Name = "Gold",
                ActivateDays = 30,
                GraceDays = 7,
                PreviousLevelId = prevId,
                PreviousLevel = new Level
                {
                    Id = prevId,
                    Name = "Silver",
                    ActivateDays = 20,
                    GraceDays = 5
                }
            }
        };

        var clientRepositoryMock = Mock.Get(clientRepository);
        var clientLevelRepositoryMock = Mock.Get(clientLevelRepository);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);
        clientLevelRepositoryMock.Setup(r => r.GetActiveByClientIdAsync(client.Id)).ReturnsAsync(clientLevel);

        var result = await clientService.GetMyActiveLevelAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(42, result!.ClientLevelId);
        Assert.Equal(clientLevel.ReceiveDate, result.ReceiveDate);
        Assert.Equal(levelId, result.Level.Id);
        Assert.Equal("Gold", result.Level.Name);
        Assert.Equal(30, result.Level.ActivateDays);
        Assert.Equal(7, result.Level.GraceDays);
        Assert.Equal(prevId, result.Level.PreviousLevelId);
        Assert.Equal("Silver", result.Level.PreviousLevelName);

        clientRepositoryMock.VerifyAll();
        clientLevelRepositoryMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task SearchPaged_MapsAndUsesSkip(
        [Frozen] IClientRepository clientRepository,
        ClientService clientService
    )
    {
        var clientId = Guid.NewGuid();
        var reg = new DateTime(2024, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var client = new Client
        {
            Id = clientId,
            UserId = "u1",
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
            BirthDate = new DateTime(1991, 2, 2),
            Gender = ClientGender.Male,
            Phone = "+70000000000",
            Email = "ivan@example.com",
            Status = ClientStatus.Active,
            RegistrationDate = reg
        };

        var clientRepositoryMock = Mock.Get(clientRepository);
        clientRepositoryMock
            .Setup(r => r.SearchPagedAsync("ив", 10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([client], 25));

        var result = await clientService.SearchPagedAsync(
            "ив",
            page: 2,
            pageSize: 10,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(25, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(clientId, result.Items[0].Id);
        Assert.Equal("Иванов", result.Items[0].LastName);
        Assert.Equal("Иван", result.Items[0].FirstName);
        Assert.Equal("Иванович", result.Items[0].MiddleName);
        Assert.Equal("ivan@example.com", result.Items[0].Email);
        Assert.Equal(ClientStatus.Active, result.Items[0].Status);
        Assert.Equal(reg, result.Items[0].RegistrationDate);

        clientRepositoryMock.VerifyAll();
    }
}