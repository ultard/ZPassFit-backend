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
    [MaxLength(20)] string Name,
    [Range(0, int.MaxValue)] int ActivateDays,
    [Range(0, int.MaxValue)] int GraceDays,
    Guid? PreviousLevelId
);

public record UpdateLevelRequest(
    [MaxLength(20)] string Name,
    [Range(0, int.MaxValue)] int ActivateDays,
    [Range(0, int.MaxValue)] int GraceDays,
    Guid? PreviousLevelId
);
