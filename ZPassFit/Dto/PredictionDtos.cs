namespace ZPassFit.Dto;

public record PredictClientRequest(
    Guid ClientId
);

public record PredictClientResponse(
    int Prediction,
    double ChurnProbability
);
