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

        var (employeeUsers, clientUsers) = await EnsureUsersAsync(userManager);

        var employees = await EnsureEmployeesAsync(db, employeeUsers);
        var clients = await EnsureClientsAsync(db, clientUsers);

        var levels = await EnsureLevelsAsync(db);
        await EnsureClientLevelsAsync(db, clients, levels);

        var plans = await EnsurePlansAsync(db);

        foreach (var client in clients)
        {
            var membership = await EnsureMembershipAsync(db, client, plans);
            await EnsureVisitsAsync(db, client, membership);
            await EnsurePaymentsAsync(db, client, employees);
            await EnsureBonusTransactionsAsync(db, client);
        }
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

    private static async Task<(List<ApplicationUser> EmployeeUsers, List<ApplicationUser> ClientUsers)> EnsureUsersAsync(
        UserManager<ApplicationUser> userManager
    )
    {
        var employeeUsers = new List<ApplicationUser>
        {
            await EnsureUserAsync(
                userManager,
                email: "employee@dev.local",
                password: "DevPassword123!",
                roles: [Roles.Employee]
            ),
            await EnsureUserAsync(
                userManager,
                email: "employee2@dev.local",
                password: "DevPassword123!",
                roles: [Roles.Employee]
            )
        };

        var clientUsers = new List<ApplicationUser>
        {
            await EnsureUserAsync(
                userManager,
                email: "client@dev.local",
                password: "DevPassword123!",
                roles: [Roles.Client]
            ),
            await EnsureUserAsync(
                userManager,
                email: "client2@dev.local",
                password: "DevPassword123!",
                roles: [Roles.Client]
            ),
            await EnsureUserAsync(
                userManager,
                email: "client3@dev.local",
                password: "DevPassword123!",
                roles: [Roles.Client]
            )
        };

        await EnsureUserAsync(
            userManager,
            email: "admin@dev.local",
            password: "DevPassword123!",
            roles: [Roles.Admin]
        );

        return (employeeUsers, clientUsers);
    }

    private static async Task<List<Employee>> EnsureEmployeesAsync(
        ApplicationDbContext db,
        List<ApplicationUser> users
    )
    {
        var res = new List<Employee>();

        foreach (var user in users)
        {
            var employee = await db.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null)
            {
                employee = user.Email switch
                {
                    "employee2@dev.local" => new Employee
                    {
                        UserId = user.Id,
                        LastName = "Кузнецова",
                        FirstName = "Мария",
                        MiddleName = "Игоревна",
                        BirthDate = UtcDate(1998, 2, 3),
                        Phone = "+79990000003",
                        Email = user.Email ?? "employee2@dev.local",
                        HireDate = DateTime.UtcNow.Date.AddDays(-120),
                        Notes = "Dev seed employee #2"
                    },
                    _ => new Employee
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
                    }
                };

                db.Employees.Add(employee);
                await db.SaveChangesAsync();
            }

            res.Add(employee);
        }

        return res;
    }

    private static async Task<List<Client>> EnsureClientsAsync(ApplicationDbContext db, List<ApplicationUser> users)
    {
        var res = new List<Client>();

        foreach (var user in users)
        {
            var client = await db.Clients.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (client == null)
            {
                client = user.Email switch
                {
                    "client2@dev.local" => new Client
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        LastName = "Петрова",
                        FirstName = "Екатерина",
                        MiddleName = "Андреевна",
                        BirthDate = UtcDate(1997, 11, 2),
                        Gender = ClientGender.Female,
                        Phone = "+79990000004",
                        Email = user.Email ?? "client2@dev.local",
                        Status = ClientStatus.Active,
                        Bonuses = 40,
                        Notes = "Dev seed client #2"
                    },
                    "client3@dev.local" => new Client
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        LastName = "Смирнов",
                        FirstName = "Денис",
                        MiddleName = "Олегович",
                        BirthDate = UtcDate(2002, 6, 18),
                        Gender = ClientGender.Male,
                        Phone = "+79990000005",
                        Email = user.Email ?? "client3@dev.local",
                        Status = ClientStatus.Active,
                        Bonuses = 220,
                        Notes = "Dev seed client #3"
                    },
                    _ => new Client
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
                    }
                };

                db.Clients.Add(client);
                await db.SaveChangesAsync();
            }

            res.Add(client);
        }

        return res;
    }

    private static async Task<List<Level>> EnsureLevelsAsync(ApplicationDbContext db)
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

    private static async Task EnsureClientLevelsAsync(
        ApplicationDbContext db,
        List<Client> clients,
        List<Level> levels
    )
    {
        var bronze = levels.First(l => l.Name == "Bronze");
        var silver = levels.First(l => l.Name == "Silver");
        var gold = levels.First(l => l.Name == "Gold");

        foreach (var client in clients)
        {
            var hasLevel = await db.ClientLevels.AnyAsync(cl => cl.ClientId == client.Id);
            if (hasLevel) continue;

            var targetLevel = client.Email switch
            {
                "client3@dev.local" => gold,
                "client2@dev.local" => silver,
                _ => bronze
            };

            db.ClientLevels.Add(new ClientLevel
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                LevelId = targetLevel.Id
            });

            await db.SaveChangesAsync();
        }
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

        var plan = client.Email switch
        {
            "client3@dev.local" => plans.Last(),
            "client2@dev.local" => plans.Skip(1).FirstOrDefault() ?? plans.First(),
            _ => plans.First()
        };

        var today = DateTime.UtcNow.Date;
        var duration = plan.Durations.Max();
        var activated = client.Email switch
        {
            "client3@dev.local" => today.AddDays(-60),
            "client2@dev.local" => today.AddDays(-25),
            _ => today.AddDays(-10)
        };

        membership = new Membership
        {
            ClientId = client.Id,
            PlanId = plan.Id,
            Status = MembershipStatus.Active,
            ActivatedDate = activated,
            ExpireDate = activated.AddDays(duration)
        };

        db.Memberships.Add(membership);
        await db.SaveChangesAsync();
        return membership;
    }

    private static async Task EnsurePaymentsAsync(ApplicationDbContext db, Client client, List<Employee> employees)
    {
        var hasAny = await db.Payments.AnyAsync(p => p.ClientId == client.Id);
        if (hasAny)
            return;

        var today = DateTime.UtcNow.Date;
        var cashier = employees.FirstOrDefault();

        var items = client.Email switch
        {
            "client3@dev.local" => new[]
            {
                new Payment
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    EmployeeId = cashier?.Id,
                    Amount = 14990,
                    Method = PaymentMethod.Card,
                    Status = PaymentStatus.Completed,
                    PaymentDate = today.AddDays(-60).AddHours(12),
                    CreateDate = today.AddDays(-60).AddHours(11)
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    EmployeeId = cashier?.Id,
                    Amount = 990,
                    Method = PaymentMethod.Cash,
                    Status = PaymentStatus.Cancelled,
                    PaymentDate = null,
                    CreateDate = today.AddDays(-5).AddHours(18)
                }
            },
            "client2@dev.local" => new[]
            {
                new Payment
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    EmployeeId = cashier?.Id,
                    Amount = 4990,
                    Method = PaymentMethod.Card,
                    Status = PaymentStatus.Completed,
                    PaymentDate = today.AddDays(-25).AddHours(9),
                    CreateDate = today.AddDays(-25).AddHours(8)
                }
            },
            _ => new[]
            {
                new Payment
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    EmployeeId = cashier?.Id,
                    Amount = 1990,
                    Method = PaymentMethod.Cash,
                    Status = PaymentStatus.Completed,
                    PaymentDate = today.AddDays(-10).AddHours(19),
                    CreateDate = today.AddDays(-10).AddHours(18).AddMinutes(50)
                },
                new Payment
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    EmployeeId = cashier?.Id,
                    Amount = 1990,
                    Method = PaymentMethod.Card,
                    Status = PaymentStatus.Pending,
                    PaymentDate = null,
                    CreateDate = today.AddDays(-1).AddHours(10)
                }
            }
        };

        db.Payments.AddRange(items);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureBonusTransactionsAsync(ApplicationDbContext db, Client client)
    {
        var hasAny = await db.BonusTransactions.AnyAsync(t => t.ClientId == client.Id);
        if (hasAny)
            return;

        var today = DateTime.UtcNow.Date;

        var items = client.Email switch
        {
            "client3@dev.local" => new[]
            {
                new BonusTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Type = BonusTransactionType.Accrual,
                    CreateDate = today.AddDays(-90).AddHours(10),
                    ExpireDate = today.AddDays(90)
                },
                new BonusTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Type = BonusTransactionType.Redeem,
                    CreateDate = today.AddDays(-7).AddHours(18),
                    ExpireDate = null
                },
                new BonusTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Type = BonusTransactionType.Adjust,
                    CreateDate = today.AddDays(-1).AddHours(12),
                    ExpireDate = null
                }
            },
            "client2@dev.local" => new[]
            {
                new BonusTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Type = BonusTransactionType.Accrual,
                    CreateDate = today.AddDays(-30).AddHours(9),
                    ExpireDate = today.AddDays(60)
                },
                new BonusTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Type = BonusTransactionType.Expire,
                    CreateDate = today.AddDays(-2).AddHours(8),
                    ExpireDate = today.AddDays(-2)
                }
            },
            _ => new[]
            {
                new BonusTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Type = BonusTransactionType.Accrual,
                    CreateDate = today.AddDays(-14).AddHours(14),
                    ExpireDate = today.AddDays(30)
                },
                new BonusTransaction
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id,
                    Type = BonusTransactionType.Redeem,
                    CreateDate = today.AddDays(-3).AddHours(20),
                    ExpireDate = null
                }
            }
        };

        db.BonusTransactions.AddRange(items);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureVisitsAsync(ApplicationDbContext db, Client client, Membership membership)
    {
        var hasAny = await db.VisitLogs.AnyAsync(v => v.ClientId == client.Id && v.MembershipId == membership.Id);
        if (hasAny)
            return;

        var today = DateTime.UtcNow.Date;

        var items = client.Email switch
        {
            "client3@dev.local" => new[]
            {
                new VisitLog
                {
                    ClientId = client.Id,
                    MembershipId = membership.Id,
                    EnterDate = today.AddDays(-21).AddHours(7),
                    LeaveDate = today.AddDays(-21).AddHours(8).AddMinutes(35)
                },
                new VisitLog
                {
                    ClientId = client.Id,
                    MembershipId = membership.Id,
                    EnterDate = today.AddDays(-14).AddHours(19),
                    LeaveDate = today.AddDays(-14).AddHours(20).AddMinutes(5)
                },
                new VisitLog
                {
                    ClientId = client.Id,
                    MembershipId = membership.Id,
                    EnterDate = today.AddDays(-3).AddHours(18),
                    LeaveDate = today.AddDays(-3).AddHours(19).AddMinutes(25)
                },
                new VisitLog
                {
                    ClientId = client.Id,
                    MembershipId = membership.Id,
                    EnterDate = today.AddHours(11),
                    LeaveDate = null
                }
            },
            "client2@dev.local" => new[]
            {
                new VisitLog
                {
                    ClientId = client.Id,
                    MembershipId = membership.Id,
                    EnterDate = today.AddDays(-10).AddHours(9),
                    LeaveDate = today.AddDays(-10).AddHours(10).AddMinutes(10)
                },
                new VisitLog
                {
                    ClientId = client.Id,
                    MembershipId = membership.Id,
                    EnterDate = today.AddDays(-6).AddHours(8),
                    LeaveDate = today.AddDays(-6).AddHours(9).AddMinutes(5)
                },
                new VisitLog
                {
                    ClientId = client.Id,
                    MembershipId = membership.Id,
                    EnterDate = today.AddDays(-1).AddHours(20),
                    LeaveDate = today.AddDays(-1).AddHours(21).AddMinutes(15)
                }
            },
            _ => new[]
            {
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
            }
        };

        db.VisitLogs.AddRange(items);

        await db.SaveChangesAsync();
    }
}

