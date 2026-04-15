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
    public async Task GetAll_Maps(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var prevId = Guid.NewGuid();
        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([
            new Level
            {
                Id = id,
                Name = "Gold",
                ActivateDays = 30,
                GraceDays = 7,
                PreviousLevelId = prevId,
                PreviousLevel = new Level { Id = prevId, Name = "Silver", ActivateDays = 20, GraceDays = 5 }
            }
        ]);

        var list = await levelService.GetAllAsync(TestContext.Current.CancellationToken);

        Assert.Single(list);
        Assert.Equal(id, list[0].Id);
        Assert.Equal("Gold", list[0].Name);
        Assert.Equal(30, list[0].ActivateDays);
        Assert.Equal(7, list[0].GraceDays);
        Assert.Equal(prevId, list[0].PreviousLevelId);
        Assert.Equal("Silver", list[0].PreviousLevelName);

        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task GetById_Missing_ReturnsNull(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Level?)null);

        var result = await levelService.GetByIdAsync(id, TestContext.Current.CancellationToken);

        Assert.Null(result);
        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Create_EmptyName_Throws(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var repoMock = Mock.Get(levelRepository);
        var ct = TestContext.Current.CancellationToken;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            levelService.CreateAsync(new CreateLevelRequest("   ", 1, 2, null), ct));

        Assert.Equal("Level name is required.", ex.Message);
        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Create_PreviousNotFound_Throws(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var prevId = Guid.NewGuid();
        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(prevId)).ReturnsAsync((Level?)null);

        var ct = TestContext.Current.CancellationToken;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            levelService.CreateAsync(new CreateLevelRequest("A", 1, 2, prevId), ct));

        Assert.Equal("Previous level not found.", ex.Message);
        repoMock.VerifyAll();
    }

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
    public async Task Update_NotFound_ReturnsNull(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Level?)null);

        var result = await levelService.UpdateAsync(
            id,
            new UpdateLevelRequest("X", 1, 2, null),
            TestContext.Current.CancellationToken);

        Assert.Null(result);
        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Update_PreviousNotFound_Throws(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var prevId = Guid.NewGuid();
        var existing = new Level
        {
            Id = id,
            Name = "A",
            ActivateDays = 1,
            GraceDays = 1,
            PreviousLevelId = null
        };

        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        repoMock.Setup(r => r.GetByIdAsync(prevId)).ReturnsAsync((Level?)null);

        var ct = TestContext.Current.CancellationToken;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            levelService.UpdateAsync(id, new UpdateLevelRequest("A", 1, 2, prevId), ct));

        Assert.Equal("Previous level not found.", ex.Message);
        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Update_SelfAsPrevious_Throws(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var existing = new Level
        {
            Id = id,
            Name = "A",
            ActivateDays = 1,
            GraceDays = 1,
            PreviousLevelId = null
        };

        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);

        var ct = TestContext.Current.CancellationToken;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            levelService.UpdateAsync(id, new UpdateLevelRequest("A", 1, 2, id), ct));

        Assert.Equal("A level cannot reference itself as previous.", ex.Message);
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

    [Theory]
    [AutoMoqData]
    public async Task Update_PersistsAndReturnsReloaded(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var existing = new Level
        {
            Id = id,
            Name = "Old",
            ActivateDays = 1,
            GraceDays = 1,
            PreviousLevelId = null
        };
        var reloaded = new Level
        {
            Id = id,
            Name = "New",
            ActivateDays = 5,
            GraceDays = 2,
            PreviousLevelId = null
        };

        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);
        repoMock.SetupSequence(r => r.GetByIdAsync(id))
            .ReturnsAsync(existing)
            .ReturnsAsync(reloaded);

        var result = await levelService.UpdateAsync(
            id,
            new UpdateLevelRequest("New", 5, 2, null),
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("New", result!.Name);
        Assert.Equal(5, result.ActivateDays);
        Assert.Equal(2, result.GraceDays);

        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Delete_NotFound_Throws(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Level?)null);

        var ct = TestContext.Current.CancellationToken;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => levelService.DeleteAsync(id, ct));

        Assert.Equal("Level not found.", ex.Message);
        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Delete_AssignedToClients_Throws(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var level = new Level { Id = id, Name = "X", ActivateDays = 1, GraceDays = 1 };
        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(level);
        repoMock.Setup(r => r.CountClientLevelsUsingLevelAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var ct = TestContext.Current.CancellationToken;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => levelService.DeleteAsync(id, ct));

        Assert.Equal("Cannot delete a level assigned to clients.", ex.Message);
        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Delete_IsPreviousForAnother_Throws(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var level = new Level { Id = id, Name = "X", ActivateDays = 1, GraceDays = 1 };
        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(level);
        repoMock.Setup(r => r.CountClientLevelsUsingLevelAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repoMock.Setup(r => r.CountLevelsWithPreviousPointingToAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var ct = TestContext.Current.CancellationToken;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => levelService.DeleteAsync(id, ct));

        Assert.Equal("Cannot delete a level that is set as previous for another level.", ex.Message);
        repoMock.VerifyAll();
    }

    [Theory]
    [AutoMoqData]
    public async Task Delete_CallsRepositoryDelete(
        [Frozen] ILevelRepository levelRepository,
        LevelService levelService
    )
    {
        var id = Guid.NewGuid();
        var level = new Level { Id = id, Name = "X", ActivateDays = 1, GraceDays = 1 };
        var repoMock = Mock.Get(levelRepository);
        repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(level);
        repoMock.Setup(r => r.CountClientLevelsUsingLevelAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repoMock.Setup(r => r.CountLevelsWithPreviousPointingToAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repoMock.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);

        await levelService.DeleteAsync(id, TestContext.Current.CancellationToken);

        repoMock.VerifyAll();
    }
}
