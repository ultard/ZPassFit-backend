using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class ClientServiceTests()
{
    [Theory, AutoMoqData]
    public async Task GetMeAsync_WhenClientMissing_ReturnsNull(Mock<IClientRepository> repo)
    {
        repo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync((Client?)null);

        var svc = new ClientService(repo.Object);
        var res = await svc.GetMeAsync("u1");

        Assert.Null(res);
        repo.VerifyAll();
    }

    [Theory, AutoMoqData]
    public async Task UpsertMeAsync_WhenClientMissing_AddsNewClient_AndReturnsMapped(Mock<IClientRepository> repo)
    {
        // Arrange
        repo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync((Client?)null);

        Client? added = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Client>()))
            .Callback<Client>(c => added = c)
            .Returns(Task.CompletedTask);

        var req = new UpsertClientMeRequest(
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: "Ivanovich",
            BirthDate: new DateTime(2000, 1, 2),
            Gender: ClientGender.Male,
            Phone: "+70000000000",
            Email: "ivan@example.com",
            Notes: "note"
        );

        var clientService = new ClientService(repo.Object);
        var before = DateTime.UtcNow;
        
        // Act
        var res = await clientService.UpsertMeAsync("u1", req);
        var after = DateTime.UtcNow;

        // Assert
        Assert.NotNull(added);
        Assert.Equal("u1", added!.UserId);
        Assert.Equal(req.LastName, added.LastName);
        Assert.Equal(req.FirstName, added.FirstName);
        Assert.Equal(req.MiddleName, added.MiddleName);
        Assert.Equal(req.BirthDate, added.BirthDate);
        Assert.Equal(req.Gender, added.Gender);
        Assert.Equal(req.Phone, added.Phone);
        Assert.Equal(req.Email, added.Email);
        Assert.Equal(req.Notes, added.Notes);

        Assert.Equal(added.Id, res.Id);
        Assert.Equal(req.LastName, res.LastName);
        Assert.Equal(req.FirstName, res.FirstName);
        Assert.Equal(req.MiddleName, res.MiddleName);
        Assert.Equal(req.BirthDate, res.BirthDate);
        Assert.Equal(req.Gender, res.Gender);
        Assert.Equal(req.Phone, res.Phone);
        Assert.Equal(req.Email, res.Email);
        Assert.True(res.RegistrationDate >= before.AddSeconds(-5) && res.RegistrationDate <= after.AddSeconds(5));

        repo.VerifyAll();
    }

    [Theory, AutoMoqData]
    public async Task UpsertMeAsync_WhenClientExists_UpdatesClient_AndReturnsMapped(Mock<IClientRepository> repo)
    {
        var existing = new Client
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            LastName = "Old",
            FirstName = "Old",
            MiddleName = "Old",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = ClientGender.Unknown,
            Phone = "0",
            Email = "old@example.com",
            Notes = null
        };

        repo.Setup(r => r.GetByUserIdAsync("u1")).ReturnsAsync(existing);
        repo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

        var req = new UpsertClientMeRequest(
            LastName: "Ivanov",
            FirstName: "Ivan",
            MiddleName: "Ivanovich",
            BirthDate: new DateTime(2000, 1, 2),
            Gender: ClientGender.Male,
            Phone: "+70000000000",
            Email: "ivan@example.com",
            Notes: "note"
        );

        var svc = new ClientService(repo.Object);
        var res = await svc.UpsertMeAsync("u1", req);

        Assert.Equal(existing.Id, res.Id);
        Assert.Equal(req.LastName, existing.LastName);
        Assert.Equal(req.FirstName, existing.FirstName);
        Assert.Equal(req.MiddleName, existing.MiddleName);
        Assert.Equal(req.BirthDate, existing.BirthDate);
        Assert.Equal(req.Gender, existing.Gender);
        Assert.Equal(req.Phone, existing.Phone);
        Assert.Equal(req.Email, existing.Email);
        Assert.Equal(req.Notes, existing.Notes);

        repo.VerifyAll();
    }

    [Theory, AutoMoqData]
    public async Task GetByIdAsync_WhenClientMissing_ReturnsNull(Mock<IClientRepository> repo)
    {
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Client?)null);

        var svc = new ClientService(repo.Object);
        var res = await svc.GetByIdAsync(Guid.NewGuid());

        Assert.Null(res);
        repo.VerifyAll();
    }
}

