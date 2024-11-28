using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassRegistrationWorker.Migrations
{
    /// <inheritdoc />
    public partial class PaymentReminderToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymenRemindertTokenId",
                table: "ClassRegistrationState",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymenRemindertTokenId",
                table: "ClassRegistrationState");
        }
    }
}
