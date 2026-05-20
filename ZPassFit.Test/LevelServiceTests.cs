using AutoFixture.Xunit3;
using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Dto;
using ZPassFit.Services.Implementations;

namespace ZPassFit.Test;

public class LevelServiceTests
{
    [Theory]
    [AutoMoqData]
    public async Task Create_AddsAndReturnsReloaded(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        Level? saved = null;
        var repoMock = Mock.Get(levelRepository);
        repoMock
            .Setup(r => r.AddAsync(It.IsAny<Level>()))
            .Callback<Level>(l => saved = l)
            .Returns(Task.CompletedTask);
        repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) =>
                saved != null && id == saved.Id
                    ? saved
                    : null);

        var result = await levelService.CreateAsync(
            new CreateLevelRequest("  Gold  ", 10, 3, null),
            TestContext.Current.CancellationToken);

        Assert.NotNull(saved);
        Assert.Equal("Gold", saved!.Name);
        Assert.Equal(10, saved.ActivateDays);
        Assert.Equal(3, saved.GraceDays);
        Assert.Null(saved.PreviousLevelId);

        Assert.Equal(saved.Id, result.Id);
        Assert.Equal("Gold", result.Name);

        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Update_ChainWouldCycle_Throws(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var levelA = new Level
        {
            Id = idA,
            Name = "A",
            ActivateDays = 1,
            GraceDays = 1,
            PreviousLevelId = null
        };
        var levelB = new Level
        {
            Id = idB,
            Name = "B",
            ActivateDays = 1,
            GraceDays = 1,
            PreviousLevelId = idA
        };

        var repoMock = Mock.Get(levelRepository);
        repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => id == idA ? levelA : id == idB ? levelB : null);

        var ct = TestContext.Current.CancellationToken;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            levelService.UpdateAsync(idA, new UpdateLevelRequest("A", 1, 2, idB), ct));

        Assert.Equal("Previous level chain would create a cycle.", ex.Message);
        repoMock.VerifyAll();
    }
}