using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarMaintenance.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fix_BookingService_Add_Id_PrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingServices",
                table: "BookingServices");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "BookingServices",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingServices",
                table: "BookingServices",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_BookingId_ServiceId",
                table: "BookingServices",
                columns: new[] { "BookingId", "ServiceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingServices",
                table: "BookingServices");

            migrationBuilder.DropIndex(
                name: "IX_BookingServices_BookingId_ServiceId",
                table: "BookingServices");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "BookingServices");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingServices",
                table: "BookingServices",
                columns: new[] { "BookingId", "ServiceId" });
        }
    }
}
