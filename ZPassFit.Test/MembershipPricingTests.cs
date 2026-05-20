using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Test;

public class MembershipPricingTests
{
    [Fact]
    public void ComputePrice_SingleDuration_ScalesProportionally()
    {
        var plan = new MembershipPlan
        {
            Name = "Base",
            Description = "Desc",
            Durations = [30],
            Price = 1000
        };

        Assert.Equal(1000, MembershipPricing.ComputePrice(plan, 30));
        Assert.Equal(2000, MembershipPricing.ComputePrice(plan, 60));
    }

    [Fact]
    public void ComputePrice_MultipleDurations_UsesMinAsBase()
    {
        var plan = new MembershipPlan
        {
            Name = "Base",
            Description = "Desc",
            Durations = [90, 30, 180],
            Price = 1000
        };

        Assert.Equal(1000, MembershipPricing.ComputePrice(plan, 30));
        Assert.Equal(3000, MembershipPricing.ComputePrice(plan, 90));
        Assert.Equal(6000, MembershipPricing.ComputePrice(plan, 180));
    }
}