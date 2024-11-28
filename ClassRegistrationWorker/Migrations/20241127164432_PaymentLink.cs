using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassRegistrationWorker.Migrations
{
    /// <inheritdoc />
    public partial class PaymentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentLink",
                table: "ClassRegistrationState",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentLink",
                table: "ClassRegistrationState");
        }
    }
}
