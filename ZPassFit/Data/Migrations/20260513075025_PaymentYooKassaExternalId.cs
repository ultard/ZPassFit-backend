using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZPassFit.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaymentYooKassaExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "YooKassaPaymentId",
                table: "Payments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YooKassaPaymentId",
                table: "Payments");
        }
    }
}
