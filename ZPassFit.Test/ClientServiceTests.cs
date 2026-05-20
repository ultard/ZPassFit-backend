using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Services.Implementations;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Test;

public class ClientServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task GetMyActiveLevel_WithNextLevel_ComputesRemaining(
        [Frozen] IClientRepository clientRepository,
        [Frozen] IClientLevelRepository clientLevelRepository,
        [Frozen] ILevelRepository levelRepository,
        [Frozen] IVisitLogRepository visitLogRepository,
        ClientService clientService
    )
    {
        var userId = "u1";
        var bronzeId = Guid.NewGuid();
        var silverId = Guid.NewGuid();
        var clientLevelId = Guid.NewGuid();
        var reg = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
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
            Email = "petrov@example.com",
            RegistrationDate = reg
        };

        var nextLevel = new Level
        {
            Id = silverId,
            Name = "Silver",
            ActivateDays = 30,
            GraceDays = 10,
            PreviousLevelId = bronzeId,
            PreviousLevel = new Level
            {
                Id = bronzeId,
                Name = "Bronze",
                ActivateDays = 0,
                GraceDays = 7
            }
        };

        var clientLevel = new ClientLevel
        {
            Id = clientLevelId,
            ClientId = client.Id,
            LevelId = bronzeId,
            ReceiveDate = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            Level = new Level
            {
                Id = bronzeId,
                Name = "Bronze",
                ActivateDays = 0,
                GraceDays = 7,
                PreviousLevelId = null,
                PreviousLevel = null
            }
        };

        var clientRepositoryMock = Mock.Get(clientRepository);
        var clientLevelRepositoryMock = Mock.Get(clientLevelRepository);
        var levelRepositoryMock = Mock.Get(levelRepository);
        var visitLogRepositoryMock = Mock.Get(visitLogRepository);
        clientRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(client);
        clientLevelRepositoryMock.Setup(r => r.GetActiveByClientIdAsync(client.Id)).ReturnsAsync(clientLevel);
        levelRepositoryMock
            .Setup(r => r.GetNextByPreviousLevelIdAsync(bronzeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nextLevel);
        visitLogRepositoryMock
            .Setup(r => r.CountDistinctVisitDaysByClientAsync(client.Id, reg, It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        var result = await clientService.GetMyActiveLevelAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(silverId, result.NextLevel!.Id);
        Assert.Equal("Silver", result.NextLevel.Name);
        Assert.Equal(30, result.NextLevel.ActivateDays);
        Assert.Equal(18, result.RemainingDaysToNextLevel);

        clientRepositoryMock.VerifyAll();
        clientLevelRepositoryMock.VerifyAll();
        levelRepositoryMock.VerifyAll();
        visitLogRepositoryMock.VerifyAll();
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
        jwtMock.Setup(j => j.RevokeAllRefreshTokensAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var ok = await clientService.BlockAsync(clientId, TestContext.Current.CancellationToken);

        Assert.True(ok);
        clientRepositoryMock.VerifyAll();
        jwtMock.Verify(j => j.RevokeAllRefreshTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreditBalance_AddsToBalance_ReturnsUpdated()
    {
        var clientRepo = new Mock<IClientRepository>();
        var clientLevelRepo = new Mock<IClientLevelRepository>();
        var levelRepo = new Mock<ILevelRepository>();
        var visitRepo = new Mock<IVisitLogRepository>();
        var jwt = new Mock<IJwtTokenService>();

        var id = Guid.NewGuid();
        var client = new Client
        {
            Id = id,
            UserId = "u",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "+70000000000",
            Email = "a@b.c",
            Balance = 500,
            Bonuses = 0
        };

        clientRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(client);
        clientRepo
            .Setup(r => r.UpdateAsync(It.Is<Client>(c => c.Id == id && c.Balance == 1500)))
            .Returns(Task.CompletedTask);

        var svc = new ClientService(
            clientRepo.Object,
            clientLevelRepo.Object,
            levelRepo.Object,
            visitRepo.Object,
            jwt.Object);

        var result = await svc.CreditBalanceAsync(id, 1000);

        Assert.NotNull(result);
        Assert.Equal(1500, result.Balance);
        clientRepo.Verify(r => r.UpdateAsync(It.IsAny<Client>()), Times.Once);
    }
}