using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MemberNameTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Member",
                schema: "Billing",
                table: "MemberTransactions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Member",
                schema: "Billing",
                table: "MemberTransactions");
        }
    }
}
