using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHasTenantTransactionDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTenant",
                schema: "Billing",
                table: "MemberTransactionDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdTenant",
                schema: "Billing",
                table: "MemberTransactionDetails");
        }
    }
}
