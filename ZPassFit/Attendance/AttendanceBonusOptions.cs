namespace ZPassFit.Attendance;

public class AttendanceBonusOptions
{
    public const string SectionName = "AttendanceBonus";

    /// <summary>Бонусов за завершённое посещение (checkout).</summary>
    public int DisciplineBonusPoints { get; set; } = 10;

    /// <summary>Срок действия начисленных бонусов (дней).</summary>
    public int BonusValidityDays { get; set; } = 90;

    /// <summary>Минимальная длительность визита для начисления (минуты).</summary>
    public int MinVisitDurationMinutes { get; set; } = 30;
}
