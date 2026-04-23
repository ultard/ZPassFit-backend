using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Implementations;
using ZPassFit.Services.Interfaces;

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
        var clientLevelId = Guid.NewGuid();
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
            Id = clientLevelId,
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
        Assert.Equal(clientLevelId, result!.ClientLevelId);
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

    [Theory]
    [AutoMoqData]
    public async Task Block_SetsBlockedAndRevokesRefresh(
        [Frozen] IClientRepository clientRepository,
        [Frozen] IJwtTokenService jwtTokenService,
        ClientService clientService
    )
    {
        var clientId = Guid.NewGuid();
        var userId = "user-1";
        var client = new Client
        {
            Id = clientId,
            UserId = userId,
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "+70000000000",
            Email = "a@b.c",
            Status = ClientStatus.Active
        };

        var clientRepositoryMock = Mock.Get(clientRepository);
        var jwtMock = Mock.Get(jwtTokenService);
        clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        clientRepositoryMock
            .Setup(r => r.UpdateAsync(It.Is<Client>(c => c.Status == ClientStatus.Blocked)))
            .Returns(Task.CompletedTask);
        jwtMock.Setup(j => j.RevokeAllRefreshTokensAsync(userId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var ok = await clientService.BlockAsync(clientId, TestContext.Current.CancellationToken);

        Assert.True(ok);
        clientRepositoryMock.VerifyAll();
        jwtMock.Verify(j => j.RevokeAllRefreshTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoMoqData]
    public async Task Block_Missing_ReturnsFalse(
        [Frozen] IClientRepository clientRepository,
        ClientService clientService
    )
    {
        var clientId = Guid.NewGuid();
        Mock.Get(clientRepository).Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync((Client?)null);

        var ok = await clientService.BlockAsync(clientId, TestContext.Current.CancellationToken);

        Assert.False(ok);
    }

    [Theory]
    [AutoMoqData]
    public async Task Unblock_SetsActive(
        [Frozen] IClientRepository clientRepository,
        ClientService clientService
    )
    {
        var clientId = Guid.NewGuid();
        var client = new Client
        {
            Id = clientId,
            UserId = "u",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "+70000000000",
            Email = "a@b.c",
            Status = ClientStatus.Blocked
        };

        var clientRepositoryMock = Mock.Get(clientRepository);
        clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId)).ReturnsAsync(client);
        clientRepositoryMock
            .Setup(r => r.UpdateAsync(It.Is<Client>(c => c.Status == ClientStatus.Active)))
            .Returns(Task.CompletedTask);

        var ok = await clientService.UnblockAsync(clientId);

        Assert.True(ok);
        clientRepositoryMock.VerifyAll();
    }
}