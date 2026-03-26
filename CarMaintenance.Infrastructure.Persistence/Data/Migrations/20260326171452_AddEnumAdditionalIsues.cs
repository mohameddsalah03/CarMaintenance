using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarMaintenance.Infrastructure.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumAdditionalIsues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "AdditionalIssues");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AdditionalIssues",
                type: "nvarchar(20)",
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "AdditionalIssues");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "AdditionalIssues",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
