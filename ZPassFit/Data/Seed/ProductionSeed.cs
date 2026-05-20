using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ZPassFit.Data.Models;
using ZPassFit.Options.Auth;

namespace ZPassFit.Data.Seed;

public static class ProductionSeed
{
    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        var env = services.GetRequiredService<IHostEnvironment>();
        if (!env.IsProduction())
            return;

        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var seedOptions = services.GetRequiredService<IOptions<SeedOptions>>().Value;

        await ReferenceDataSeed.EnsureRolesAsync(roleManager);
        await ReferenceDataSeed.EnsureLevelsAsync(db);
        await ReferenceDataSeed.EnsurePlansAsync(db);
        await EnsureAdminUserAsync(userManager, seedOptions);
    }

    private static async Task EnsureAdminUserAsync(UserManager<ApplicationUser> userManager, SeedOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
            return;

        var email = options.AdminEmail.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            var create = await userManager.CreateAsync(user, options.AdminPassword);
            if (!create.Succeeded)
                throw new InvalidOperationException("Failed to create admin user: " + email);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var add = await userManager.AddToRoleAsync(user, Roles.Admin);
            if (!add.Succeeded)
                throw new InvalidOperationException($"Failed to add Admin role to {email}");
        }
    }
}