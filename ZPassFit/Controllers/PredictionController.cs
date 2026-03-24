using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZPassFit.Auth;
using ZPassFit.Dto;
using ZPassFit.Services.Interfaces;

namespace ZPassFit.Controllers;

[Authorize]
[ApiController]
[Tags("AI")]
[Route("[controller]")]
public class PredictionController(
    IPredictionService predictionService
) : ControllerBase
{
    [HttpPost("predict")]
    [Authorize(Roles = Roles.AdminOrEmployee)]
    [EndpointSummary("Предсказать отток")]
    [EndpointDescription("Собирает данные клиента из БД, вызывает gRPC PredictionService и возвращает предсказание.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PredictClientResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IResult> Predict([FromBody] PredictClientRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var prediction = await predictionService.PredictAsync(request.ClientId, cancellationToken);
            return prediction == null
                ? Results.NotFound(new { error = "Client or membership not found." })
                : Results.Ok(prediction);
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title: "Prediction service unavailable.",
                detail: exception.Message,
                statusCode: StatusCodes.Status502BadGateway
            );
        }
    }
}
