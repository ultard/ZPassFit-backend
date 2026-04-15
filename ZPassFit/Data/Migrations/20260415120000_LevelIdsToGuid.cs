using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZPassFit.Data.Migrations;

/// <inheritdoc />
public partial class LevelIdsToGuid : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ClientLevels_Levels_LevelId",
            table: "ClientLevels");

        migrationBuilder.DropForeignKey(
            name: "FK_Levels_Levels_PreviousLevelId",
            table: "Levels");

        migrationBuilder.DropTable(
            name: "ClientLevels");

        migrationBuilder.DropTable(
            name: "Levels");

        migrationBuilder.CreateTable(
            name: "Levels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ActivateDays = table.Column<int>(type: "integer", nullable: false),
                GraceDays = table.Column<int>(type: "integer", nullable: false),
                PreviousLevelId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Levels", x => x.Id);
                table.ForeignKey(
                    name: "FK_Levels_Levels_PreviousLevelId",
                    column: x => x.PreviousLevelId,
                    principalTable: "Levels",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "ClientLevels",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                LevelId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientLevels", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientLevels_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ClientLevels_Levels_LevelId",
                    column: x => x.LevelId,
                    principalTable: "Levels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ClientLevels_ClientId",
            table: "ClientLevels",
            column: "ClientId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ClientLevels_LevelId",
            table: "ClientLevels",
            column: "LevelId");

        migrationBuilder.CreateIndex(
            name: "IX_Levels_PreviousLevelId",
            table: "Levels",
            column: "PreviousLevelId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "Откат миграции LevelIdsToGuid не поддерживается: восстановление int-ключей уровней невозможно без потери данных.");
    }
}
