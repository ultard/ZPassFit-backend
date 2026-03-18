using System.ComponentModel.DataAnnotations;

namespace ZPassFit.Data.Models.Clients;

public class Level
{
    public int Id { get; set; }

    [MaxLength(20)]
    public required string Name { get; set; }
    
    [Range(0, int.MaxValue)]
    public int ActivateDays { get; set; }
    
    [Range(0, int.MaxValue)]
    public int GraceDays { get; set; }

    public int? PreviousLevelId { get; set; }
    public Level? PreviousLevel { get; set; }
}