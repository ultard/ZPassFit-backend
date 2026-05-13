using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Test;

public class MembershipPricingTests
{
    [Fact]
    public void ComputePrice_NoDurations_ReturnsBasePrice()
    {
        var plan = new MembershipPlan
        {
            Name = "Base",
            Description = "Desc",
            Durations = [],
            Price = 1000
        };

        Assert.Equal(1000, MembershipPricing.ComputePrice(plan, 30));
        Assert.Equal(1000, MembershipPricing.ComputePrice(plan, 365));
    }

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

    [Fact]
    public void ComputePrice_NonMultipleDuration_RoundsToNearestRuble()
    {
        var plan = new MembershipPlan
        {
            Name = "Base",
            Description = "Desc",
            Durations = [30],
            Price = 1000
        };

        Assert.Equal(1500, MembershipPricing.ComputePrice(plan, 45));
        Assert.Equal(1333, MembershipPricing.ComputePrice(plan, 40));
    }

    [Fact]
    public void ComputePrice_InvalidInputs_ReturnsBasePrice()
    {
        var plan = new MembershipPlan
        {
            Name = "Base",
            Description = "Desc",
            Durations = [30],
            Price = 1000
        };

        Assert.Equal(1000, MembershipPricing.ComputePrice(plan, 0));
        Assert.Equal(1000, MembershipPricing.ComputePrice(plan, -5));
    }

    [Fact]
    public void MinDuration_NoDurations_ReturnsZero()
    {
        var plan = new MembershipPlan
        {
            Name = "Base",
            Description = "Desc",
            Durations = [],
            Price = 1000
        };

        Assert.Equal(0, MembershipPricing.MinDuration(plan));
    }

    [Fact]
    public void MinDuration_ReturnsMinimumPositiveValue()
    {
        var plan = new MembershipPlan
        {
            Name = "Base",
            Description = "Desc",
            Durations = [90, 30, 180],
            Price = 1000
        };

        Assert.Equal(30, MembershipPricing.MinDuration(plan));
    }
}
