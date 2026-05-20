using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;
using ZPassFit.Options.Auth;

namespace ZPassFit.Data.Seed;

public static class ReferenceDataSeed
{
    public static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { Roles.Admin, Roles.Employee, Roles.Client })
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var res = await roleManager.CreateAsync(new IdentityRole(role));
            if (!res.Succeeded)
                throw new InvalidOperationException("Failed to create role: " + role);
        }
    }

    public static async Task<List<Level>> EnsureLevelsAsync(ApplicationDbContext db)
    {
        var levelSpecs = new List<(string Name, int ActivateDays, int GraceDays, string? Previous)>
        {
            ("Bronze", 0, 7, null),
            ("Silver", 30, 10, "Bronze"),
            ("Gold", 90, 14, "Silver")
        };

        foreach (var spec in levelSpecs)
        {
            var exists = await db.Levels.AnyAsync(l => l.Name == spec.Name);
            if (exists) continue;

            db.Levels.Add(new Level
            {
                Name = spec.Name,
                ActivateDays = spec.ActivateDays,
                GraceDays = spec.GraceDays
            });
        }

        await db.SaveChangesAsync();

        var levels = await db.Levels
            .Where(l => levelSpecs.Select(s => s.Name).Contains(l.Name))
            .OrderBy(l => l.ActivateDays)
            .ToListAsync();

        var byName = levels.ToDictionary(l => l.Name, l => l);
        var changed = false;
        foreach (var spec in levelSpecs)
        {
            if (spec.Previous == null) continue;
            var level = byName[spec.Name];
            var prev = byName[spec.Previous];
            if (level.PreviousLevelId != prev.Id)
            {
                level.PreviousLevelId = prev.Id;
                changed = true;
            }
        }

        if (changed)
        {
            db.Levels.UpdateRange(levels);
            await db.SaveChangesAsync();
        }

        return levels;
    }

    public static async Task<List<MembershipPlan>> EnsurePlansAsync(ApplicationDbContext db)
    {
        var planSpecs = new[]
        {
            new { Name = "Basic", Description = "30 дней, 1 клуб", Durations = new[] { 30 }, Price = 1990 },
            new { Name = "Standard", Description = "90 дней, 1 клуб", Durations = new[] { 90 }, Price = 4990 },
            new { Name = "Pro", Description = "365 дней, все клубы", Durations = new[] { 365 }, Price = 14990 }
        };

        foreach (var spec in planSpecs)
        {
            var exists = await db.MembershipPlans.AnyAsync(p => p.Name == spec.Name);
            if (exists) continue;

            db.MembershipPlans.Add(new MembershipPlan
            {
                Name = spec.Name,
                Description = spec.Description,
                Durations = spec.Durations,
                Price = spec.Price
            });
        }

        await db.SaveChangesAsync();

        return await db.MembershipPlans
            .Where(p => planSpecs.Select(s => s.Name).Contains(p.Name))
            .OrderBy(p => p.Id)
            .ToListAsync();
    }
}