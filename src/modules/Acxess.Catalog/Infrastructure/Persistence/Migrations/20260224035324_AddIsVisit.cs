using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "Catalog",
                table: "AddOns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisit",
                schema: "Catalog",
                table: "AddOns",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "Catalog",
                table: "AddOns");

            migrationBuilder.DropColumn(
                name: "IsVisit",
                schema: "Catalog",
                table: "AddOns");
        }
    }
}
