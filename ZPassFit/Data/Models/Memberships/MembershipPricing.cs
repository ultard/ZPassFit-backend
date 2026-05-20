namespace ZPassFit.Data.Models.Memberships;

public static class MembershipPricing
{
    public static int ComputePrice(MembershipPlan plan, int durationDays)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (durationDays <= 0 || plan.Price <= 0 || plan.Durations.Length == 0)
            return plan.Price;

        var minDuration = MinDuration(plan);
        if (minDuration <= 0)
            return plan.Price;

        var total = (long)plan.Price * durationDays + minDuration / 2;
        return (int)(total / minDuration);
    }

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