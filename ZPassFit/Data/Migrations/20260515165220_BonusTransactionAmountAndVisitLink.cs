using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZPassFit.Data.Migrations
{
    /// <inheritdoc />
    public partial class BonusTransactionAmountAndVisitLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Amount",
                table: "BonusTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedTransactionId",
                table: "BonusTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VisitLogId",
                table: "BonusTransactions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "BonusTransactions");

            migrationBuilder.DropColumn(
                name: "RelatedTransactionId",
                table: "BonusTransactions");

            migrationBuilder.DropColumn(
                name: "VisitLogId",
                table: "BonusTransactions");
        }
    }
}
