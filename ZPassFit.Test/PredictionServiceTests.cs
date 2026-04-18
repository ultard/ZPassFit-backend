using Grpc.Core;
using Moq;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Protos;
using PredictionServiceImpl = ZPassFit.Services.Implementations.PredictionService;
using ProtoPrediction = ZPassFit.Protos.PredictionService;

namespace ZPassFit.Test;

public class PredictionServiceTests
{
    [Fact]
    public async Task Predict_ClientMissing_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var grpc = new Mock<ProtoPrediction.PredictionServiceClient>(MockBehavior.Strict);
        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        var membershipRepo = new Mock<IMembershipRepository>(MockBehavior.Strict);
        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);

        clientRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Client?)null);

        var sut = new PredictionServiceImpl(grpc.Object, clientRepo.Object, membershipRepo.Object, visitRepo.Object);

        var result = await sut.PredictChurnAsync(id, TestContext.Current.CancellationToken);

        Assert.Null(result);
        clientRepo.VerifyAll();
    }

    [Fact]
    public async Task Predict_MembershipMissing_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var client = new Client
        {
            Id = id,
            UserId = "u",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 5, 5),
            Gender = ClientGender.Female,
            Phone = "+7",
            Email = "e@e.com"
        };

        var grpc = new Mock<ProtoPrediction.PredictionServiceClient>(MockBehavior.Strict);
        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        var membershipRepo = new Mock<IMembershipRepository>(MockBehavior.Strict);
        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);

        clientRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(client);
        membershipRepo.Setup(r => r.GetByClientIdAsync(id)).ReturnsAsync((Membership?)null);

        var sut = new PredictionServiceImpl(grpc.Object, clientRepo.Object, membershipRepo.Object, visitRepo.Object);

        var result = await sut.PredictChurnAsync(id, TestContext.Current.CancellationToken);

        Assert.Null(result);
        clientRepo.VerifyAll();
        membershipRepo.VerifyAll();
    }

    [Fact]
    public async Task Predict_GrpcFails_Wraps()
    {
        var id = Guid.NewGuid();
        var client = new Client
        {
            Id = id,
            UserId = "u",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 5, 5),
            Gender = ClientGender.Unknown,
            Phone = "+7",
            Email = "e@e.com"
        };

        var membership = new Membership
        {
            Id = 1,
            ClientId = id,
            PlanId = 2,
            Status = MembershipStatus.Active,
            ActivatedDate = DateTime.UtcNow.AddDays(-30),
            ExpireDate = DateTime.UtcNow.AddDays(30),
            Plan = new MembershipPlan
            {
                Id = 2,
                Name = "P",
                Description = "D",
                Durations = [30],
                Price = 1000
            },
            Client = client
        };

        var grpc = new Mock<ProtoPrediction.PredictionServiceClient>(MockBehavior.Strict);
        grpc
            .Setup(c => c.PredictChurnAsync(It.IsAny<PredictChurnRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "down")));

        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        var membershipRepo = new Mock<IMembershipRepository>(MockBehavior.Strict);
        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);

        clientRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(client);
        membershipRepo.Setup(r => r.GetByClientIdAsync(id)).ReturnsAsync(membership);
        visitRepo.Setup(r => r.GetVisitHistoryByClientIdAsync(id)).ReturnsAsync([]);

        var sut = new PredictionServiceImpl(grpc.Object, clientRepo.Object, membershipRepo.Object, visitRepo.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.PredictChurnAsync(id, TestContext.Current.CancellationToken));

        Assert.Equal("Prediction service unavailable.", ex.Message);
        Assert.NotNull(ex.InnerException);

        clientRepo.VerifyAll();
        membershipRepo.VerifyAll();
        visitRepo.VerifyAll();
        grpc.VerifyAll();
    }

    [Fact]
    public async Task Predict_Success_ReturnsGrpcResponse()
    {
        var id = Guid.NewGuid();
        var client = new Client
        {
            Id = id,
            UserId = "u",
            LastName = "A",
            FirstName = "B",
            MiddleName = "C",
            BirthDate = new DateTime(1990, 5, 5),
            Gender = ClientGender.Male,
            Phone = "+7",
            Email = "e@e.com"
        };

        var membership = new Membership
        {
            Id = 1,
            ClientId = id,
            PlanId = 2,
            Status = MembershipStatus.Active,
            ActivatedDate = DateTime.UtcNow.AddDays(-30),
            ExpireDate = DateTime.UtcNow.AddDays(30),
            Plan = new MembershipPlan
            {
                Id = 2,
                Name = "P",
                Description = "D",
                Durations = [30],
                Price = 1000
            },
            Client = client
        };

        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        var membershipRepo = new Mock<IMembershipRepository>(MockBehavior.Strict);
        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);

        clientRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(client);
        membershipRepo.Setup(r => r.GetByClientIdAsync(id)).ReturnsAsync(membership);
        visitRepo.Setup(r => r.GetVisitHistoryByClientIdAsync(id)).ReturnsAsync([]);

        var grpcClient = new FakePredictionClient
        {
            Response = new PredictChurnResponse { Prediction = 2, Probability = 0.42 }
        };

        var sut = new PredictionServiceImpl(grpcClient, clientRepo.Object, membershipRepo.Object, visitRepo.Object);

        var result = await sut.PredictChurnAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Prediction);
        Assert.Equal(0.42, result.Probability);

        clientRepo.VerifyAll();
        membershipRepo.VerifyAll();
        visitRepo.VerifyAll();
    }

    private sealed class FakePredictionClient : ProtoPrediction.PredictionServiceClient
    {
        public required PredictChurnResponse Response { get; init; }

        public override AsyncUnaryCall<PredictChurnResponse> PredictChurnAsync(
            PredictChurnRequest request,
            Metadata? headers = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            return new AsyncUnaryCall<PredictChurnResponse>(
                Task.FromResult(Response),
                Task.FromResult(Metadata.Empty),
                () => Status.DefaultSuccess,
                () => Metadata.Empty,
                () => { });
        }
    }
}
