using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class ClientServiceTests
{
    [Theory, AutoMoqData]
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

    [Theory, AutoMoqData]
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
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: "Ivanovich",
            BirthDate: new DateTime(2000, 1, 2),
            Gender: ClientGender.Male,
            Phone: "+70000000000",
            Email: "ivan@example.com",
            Notes: "note"
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

    [Theory, AutoMoqData]
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
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: "Ivanovich",
            BirthDate: new DateTime(2000, 1, 2),
            Gender: ClientGender.Male,
            Phone: "+70000000000",
            Email: "ivan@example.com",
            Notes: "note"
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

    [Theory, AutoMoqData]
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
}

