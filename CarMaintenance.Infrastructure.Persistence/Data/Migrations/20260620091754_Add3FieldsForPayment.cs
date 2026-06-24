using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarMaintenance.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add3FieldsForPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProcessedByUserId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymobTransactionId",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentProcessedByUserId",
                table: "Bookings",
                column: "PaymentProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymobTransactionId",
                table: "Bookings",
                column: "PaymobTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_PaymentProcessedByUserId",
                table: "Bookings",
                column: "PaymentProcessedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_PaymentProcessedByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentProcessedByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymobTransactionId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymentProcessedByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymobTransactionId",
                table: "Bookings");
        }
    }
}
