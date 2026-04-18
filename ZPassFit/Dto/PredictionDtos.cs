namespace ZPassFit.Dto;

public record ChurnPredictionRequest(
    Guid ClientId
);

public record ChurnPredictionResponse(
    int Prediction,
    double Probability
);
