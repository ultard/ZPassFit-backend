using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZPassFit.Auth;
using ZPassFit.Data.Models;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data.Dev;

public static class DevelopmentSeed
{
    private static DateTime UtcDate(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        var env = services.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
            return;

        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRolesAsync(roleManager);

        var employeeUser = await EnsureUserAsync(
            userManager,
            email: "employee@dev.local",
            password: "DevPassword123!",
            roles: [Roles.Employee]
        );

        var clientUser = await EnsureUserAsync(
            userManager,
            email: "client@dev.local",
            password: "DevPassword123!",
            roles: [Roles.Client]
        );

        await EnsureUserAsync(
            userManager,
            email: "admin@dev.local",
            password: "DevPassword123!",
            roles: [Roles.Admin]
        );

        await EnsureEmployeeAsync(db, employeeUser);
        var client = await EnsureClientAsync(db, clientUser);

        var plans = await EnsurePlansAsync(db);
        var membership = await EnsureMembershipAsync(db, client, plans);
        await EnsureVisitsAsync(db, client, membership);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
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

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string[] roles
    )
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email };
            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
                throw new InvalidOperationException("Failed to create user: " + email);
        }

        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                var add = await userManager.AddToRoleAsync(user, role);
                if (!add.Succeeded)
                    throw new InvalidOperationException($"Failed to add role {role} to {email}");
            }
        }

        return user;
    }

    private static async Task EnsureEmployeeAsync(ApplicationDbContext db, ApplicationUser user)
    {
        var exists = await db.Employees.AnyAsync(e => e.UserId == user.Id);
        if (exists)
            return;

        db.Employees.Add(new Employee
        {
            UserId = user.Id,
            LastName = "Сидоров",
            FirstName = "Иван",
            MiddleName = "Петрович",
            BirthDate = UtcDate(1995, 5, 12),
            Phone = "+79990000001",
            Email = user.Email ?? "employee@dev.local",
            HireDate = DateTime.UtcNow.Date.AddDays(-30),
            Notes = "Dev seed employee"
        });

        await db.SaveChangesAsync();
    }

    private static async Task<Client> EnsureClientAsync(ApplicationDbContext db, ApplicationUser user)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.UserId == user.Id);
        if (client != null)
            return client;

        client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            LastName = "Иванов",
            FirstName = "Алексей",
            MiddleName = "Сергеевич",
            BirthDate = UtcDate(2000, 1, 20),
            Gender = ClientGender.Male,
            Phone = "+79990000002",
            Email = user.Email ?? "client@dev.local",
            Status = ClientStatus.Active,
            Bonuses = 120,
            Notes = "Dev seed client"
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client;
    }

    private static async Task<List<MembershipPlan>> EnsurePlansAsync(ApplicationDbContext db)
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

    private static async Task<Membership> EnsureMembershipAsync(
        ApplicationDbContext db,
        Client client,
        List<MembershipPlan> plans
    )
    {
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.ClientId == client.Id);
        if (membership != null)
            return membership;

        var plan = plans.First();
        membership = new Membership
        {
            ClientId = client.Id,
            PlanId = plan.Id,
            Status = MembershipStatus.Active,
            ActivatedDate = DateTime.UtcNow.Date.AddDays(-10),
            ExpireDate = DateTime.UtcNow.Date.AddDays(20)
        };

        db.Memberships.Add(membership);
        await db.SaveChangesAsync();
        return membership;
    }

    private static async Task EnsureVisitsAsync(ApplicationDbContext db, Client client, Membership membership)
    {
        var hasAny = await db.VisitLogs.AnyAsync(v => v.ClientId == client.Id && v.MembershipId == membership.Id);
        if (hasAny)
            return;

        var today = DateTime.UtcNow.Date;

        db.VisitLogs.AddRange(
            new VisitLog
            {
                ClientId = client.Id,
                MembershipId = membership.Id,
                EnterDate = today.AddDays(-2).AddHours(18),
                LeaveDate = today.AddDays(-2).AddHours(19).AddMinutes(10)
            },
            new VisitLog
            {
                ClientId = client.Id,
                MembershipId = membership.Id,
                EnterDate = today.AddDays(-1).AddHours(9),
                LeaveDate = today.AddDays(-1).AddHours(10).AddMinutes(5)
            },
            new VisitLog
            {
                ClientId = client.Id,
                MembershipId = membership.Id,
                EnterDate = today.AddHours(12),
                LeaveDate = null
            }
        );

        await db.SaveChangesAsync();
    }
}

