namespace ZPassFit.Data.Models.Memberships;

/// <summary>
///     Цена тарифа указывается за <b>минимальный</b> срок из <see cref="MembershipPlan.Durations" />.
///     Для других сроков стоимость пересчитывается пропорционально длительности.
/// </summary>
public static class MembershipPricing
{
    /// <summary>
    ///     Возвращает итоговую стоимость тарифа для указанной длительности (в днях).
    /// </summary>
    public static int ComputePrice(MembershipPlan plan, int durationDays)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (durationDays <= 0 || plan.Price <= 0)
            return plan.Price;

        if (plan.Durations.Length == 0)
            return plan.Price;

        var minDuration = MinDuration(plan);
        if (minDuration <= 0)
            return plan.Price;

        // Целочисленная математика: для допустимых длительностей (кратных минимуму)
        // получаем точное значение; для произвольных — округляем до ближайшего рубля.
        var total = (long)plan.Price * durationDays + minDuration / 2;
        return (int)(total / minDuration);
    }

    /// <summary>
    ///     Наименьший допустимый срок тарифа (в днях). 0, если сроки не заданы.
    /// </summary>
    public static int MinDuration(MembershipPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Durations.Length == 0)
            return 0;

        var min = plan.Durations[0];
        foreach (var d in plan.Durations)
            if (d > 0 && (min <= 0 || d < min))
                min = d;
        return min > 0 ? min : 0;
    }
}
