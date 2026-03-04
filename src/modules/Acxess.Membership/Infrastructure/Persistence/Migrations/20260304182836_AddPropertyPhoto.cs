using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Membership.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                schema: "Membership",
                table: "Members",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                schema: "Membership",
                table: "Members");
        }
    }
}
