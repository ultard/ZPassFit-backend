using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ZPassFit.Data.Models;
using ZPassFit.Data.Models.Audit;

namespace ZPassFit.Data.Audit;

/// <summary>
/// Перед сохранением добавляет строки в <see cref="AuditLog"/> по изменённым сущностям.
/// </summary>
public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "TokenHash"
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        AppendAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        AppendAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendAuditLogs(DbContext? context)
    {
        if (context is not ApplicationDbContext db)
            return;

        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        if (ip is { Length: > 45 })
            ip = ip[..45];

        var now = DateTime.UtcNow;

        foreach (var entry in db.ChangeTracker
                     .Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                     .Where(e => !ShouldSkipEntity(e))
                     .ToList())
        {
            var action = entry.State switch
            {
                EntityState.Added => "Insert",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => "Unknown"
            };

            if (entry.State == EntityState.Modified)
            {
                var modified = entry
                    .Properties.Where(p => p.IsModified && !SensitivePropertyNames.Contains(p.Metadata.Name))
                    .ToList();
                if (modified.Count == 0)
                    continue;
            }

            var entityType = FormatEntityType(entry);
            var entityId = Truncate(FormatEntityKey(entry), 128);
            var details = BuildDetails(entry);

            db.AuditLogs.Add(
                new AuditLog
                {
                    OccurredAtUtc = now,
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = Truncate(details, 12_000),
                    IpAddress = ip
                }
            );
        }
    }

    private static bool ShouldSkipEntity(EntityEntry entry)
    {
        var type = entry.Metadata.ClrType;
        if (type == typeof(AuditLog))
            return true;
        if (type == typeof(RefreshToken))
            return true;

        var name = type.Name;
        if (name.StartsWith("IdentityUserToken", StringComparison.Ordinal))
            return true;
        if (name.StartsWith("IdentityUserLogin", StringComparison.Ordinal))
            return true;
        if (name.StartsWith("IdentityUserClaim", StringComparison.Ordinal))
            return true;
        if (name.StartsWith("IdentityRoleClaim", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static string FormatEntityType(EntityEntry entry)
    {
        var full = entry.Metadata.ClrType.FullName ?? entry.Metadata.Name;
        return Truncate(full, 256);
    }

    private static string? FormatEntityKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key == null)
            return null;

        var parts = new List<string>();
        foreach (var prop in key.Properties)
        {
            object? value = entry.State == EntityState.Deleted
                ? entry.Property(prop.Name).OriginalValue
                : entry.Property(prop.Name).CurrentValue;
            parts.Add(value?.ToString() ?? "");
        }

        return string.Join("|", parts);
    }

    private static string? BuildDetails(EntityEntry entry)
    {
        return entry.State switch
        {
            EntityState.Added => SerializeDictionary(CollectCurrentScalars(entry)),
            EntityState.Deleted => SerializeDictionary(
                new Dictionary<string, object?> { ["key"] = FormatEntityKey(entry) }
            ),
            EntityState.Modified => SerializeChanges(entry),
            _ => null
        };
    }

    private static string? SerializeChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties.Where(p =>
                     p.IsModified && !SensitivePropertyNames.Contains(p.Metadata.Name)
                 ))
        {
            changes[prop.Metadata.Name] = new
            {
                old = prop.OriginalValue,
                @new = prop.CurrentValue
            };
        }

        return changes.Count == 0 ? null : SerializeDictionary(changes);
    }

    private static Dictionary<string, object?> CollectCurrentScalars(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (SensitivePropertyNames.Contains(prop.Metadata.Name))
                continue;
            dict[prop.Metadata.Name] = prop.CurrentValue;
        }

        return dict;
    }

    private static string SerializeDictionary(Dictionary<string, object?> data)
    {
        try
        {
            return JsonSerializer.Serialize(data, JsonOptions);
        }
        catch
        {
            return "{\"error\":\"serialization failed\"}";
        }
    }

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLen)
            return value ?? "";
        return value[..maxLen] + "…";
    }
}
