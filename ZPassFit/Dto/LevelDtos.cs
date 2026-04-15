using System.ComponentModel.DataAnnotations;

namespace ZPassFit.Dto;

public record LevelResponse(
    Guid Id,
    string Name,
    int ActivateDays,
    int GraceDays,
    Guid? PreviousLevelId,
    string? PreviousLevelName
);

public record CreateLevelRequest(
    [property: MaxLength(20)] string Name,
    [property: Range(0, int.MaxValue)] int ActivateDays,
    [property: Range(0, int.MaxValue)] int GraceDays,
    Guid? PreviousLevelId
);

public record UpdateLevelRequest(
    [property: MaxLength(20)] string Name,
    [property: Range(0, int.MaxValue)] int ActivateDays,
    [property: Range(0, int.MaxValue)] int GraceDays,
    Guid? PreviousLevelId
);
