using Grpc.Core;
using Moq;
using ZPassFit.Data.Models.Attendance;
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
    public async Task Predict_MultipleVisitsSameDay_CountsDistinctDays()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
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
            Id = Guid.NewGuid(),
            ClientId = id,
            PlanId = Guid.NewGuid(),
            Status = MembershipStatus.Active,
            ActivatedDate = now.AddDays(-30),
            ExpireDate = now.AddDays(30),
            Plan = new MembershipPlan
            {
                Id = Guid.NewGuid(),
                Name = "P",
                Description = "D",
                Durations = [30],
                Price = 1000
            },
            Client = client
        };

        var sameDay = now.AddDays(-1).Date.AddHours(10);
        var visits = new List<VisitLog>
        {
            new() { Id = Guid.NewGuid(), ClientId = id, MembershipId = membership.Id, EnterDate = sameDay },
            new()
            {
                Id = Guid.NewGuid(), ClientId = id, MembershipId = membership.Id, EnterDate = sameDay.AddHours(4)
            },
            new()
            {
                Id = Guid.NewGuid(), ClientId = id, MembershipId = membership.Id, EnterDate = now.AddDays(-2)
            }
        };

        PredictChurnRequest? capturedRequest = null;
        var grpcClient = new FakePredictionClient
        {
            Response = new PredictChurnResponse { Prediction = 0, Probability = 0.1 },
            OnPredict = request => capturedRequest = request
        };

        var clientRepo = new Mock<IClientRepository>(MockBehavior.Strict);
        var membershipRepo = new Mock<IMembershipRepository>(MockBehavior.Strict);
        var visitRepo = new Mock<IVisitLogRepository>(MockBehavior.Strict);

        clientRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(client);
        membershipRepo.Setup(r => r.GetByClientIdAsync(id)).ReturnsAsync(membership);
        visitRepo.Setup(r => r.GetVisitHistoryByClientIdAsync(id)).ReturnsAsync(visits);

        var sut = new PredictionServiceImpl(grpcClient, clientRepo.Object, membershipRepo.Object, visitRepo.Object);

        var result = await sut.PredictChurnAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(2, capturedRequest!.VisitsLast7D);
        Assert.Equal(2, capturedRequest.VisitsLast4W);
        Assert.Equal(0.5, capturedRequest.VisitsPerWeek);
        Assert.Equal(1, capturedRequest.DaysSinceLastVisit);
    }

    private sealed class FakePredictionClient : ProtoPrediction.PredictionServiceClient
    {
        public required PredictChurnResponse Response { get; init; }
        public Action<PredictChurnRequest>? OnPredict { get; init; }

        public override AsyncUnaryCall<PredictChurnResponse> PredictChurnAsync(
            PredictChurnRequest request,
            Metadata? headers = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            OnPredict?.Invoke(request);

            return new AsyncUnaryCall<PredictChurnResponse>(
                Task.FromResult(Response),
                Task.FromResult(Metadata.Empty),
                () => Status.DefaultSuccess,
                () => Metadata.Empty,
                () => { });
        }
    }
}