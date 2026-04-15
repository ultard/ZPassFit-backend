using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models;
using ZPassFit.Data.Models.Attendance;
using ZPassFit.Data.Models.Audit;
using ZPassFit.Data.Models.Clients;
using ZPassFit.Data.Models.Memberships;

namespace ZPassFit.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<Employee> Employees { get; set; }

    public DbSet<Client> Clients { get; set; }
    public DbSet<Level> Levels { get; set; }
    public DbSet<ClientLevel> ClientLevels { get; set; }
    public DbSet<BonusTransaction> BonusTransactions { get; set; }

    public DbSet<MembershipPlan> MembershipPlans { get; set; }
    public DbSet<Membership> Memberships { get; set; }
    public DbSet<Payment> Payments { get; set; }

    public DbSet<QrSession> QrSessions { get; set; }
    public DbSet<VisitLog> VisitLogs { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.OccurredAtUtc);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}