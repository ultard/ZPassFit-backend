using Microsoft.EntityFrameworkCore;
using ZPassFit.Data.Models.Clients;

namespace ZPassFit.Data.Repositories.Clients;

public class ClientLevelRepository(ApplicationDbContext context) : IClientLevelRepository
{
    public async Task<ClientLevel?> GetByIdAsync(Guid id)
    {
        return await context.ClientLevels
            .Include(cl => cl.Client)
            .Include(cl => cl.Level)
            .FirstOrDefaultAsync(cl => cl.Id == id);
    }

    public async Task<ClientLevel?> GetActiveByClientIdAsync(Guid clientId)
    {
        return await context.ClientLevels
            .Include(cl => cl.Level)
            .ThenInclude(l => l.PreviousLevel)
            .FirstOrDefaultAsync(cl => cl.ClientId == clientId && cl.RevocationDate == null);
    }

    public async Task AddAsync(ClientLevel clientLevel)
    {
        await context.ClientLevels.AddAsync(clientLevel);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ClientLevel clientLevel)
    {
        context.ClientLevels.Update(clientLevel);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var clientLevel = await GetByIdAsync(id);
        if (clientLevel == null) return;

        context.ClientLevels.Remove(clientLevel);
        await context.SaveChangesAsync();
    }

    public async Task<int> ResetLevelsWithExpiredGraceAsync(CancellationToken cancellationToken = default)
    {
        var sql =
            """
            WITH last_visits AS (
                SELECT v."ClientId", MAX(v."EnterDate") AS "LastEnterDate"
                FROM "VisitLogs" AS v
                GROUP BY v."ClientId"
            ),
            expired AS (
                SELECT cl."Id", l."PreviousLevelId" AS "PrevLevelId"
                FROM "ClientLevels" AS cl
                JOIN "Levels" AS l ON l."Id" = cl."LevelId"
                LEFT JOIN last_visits AS lv ON lv."ClientId" = cl."ClientId"
                WHERE cl."RevocationDate" IS NULL
                  AND l."GraceDays" > 0
                  AND l."PreviousLevelId" IS NOT NULL
                  AND COALESCE(lv."LastEnterDate", cl."ReceiveDate")
                        < (now() AT TIME ZONE 'utc') - (l."GraceDays" * INTERVAL '1 day')
            )
            UPDATE "ClientLevels" AS cl
            SET "LevelId" = e."PrevLevelId",
                "ReceiveDate" = (now() AT TIME ZONE 'utc'),
                "RevocationDate" = NULL
            FROM expired AS e
            WHERE cl."Id" = e."Id";
            """;

        return await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}