using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZPassFit.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClientBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenewEnabled",
                table: "Memberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Balance",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoRenewEnabled",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "Clients");
        }
    }
}
