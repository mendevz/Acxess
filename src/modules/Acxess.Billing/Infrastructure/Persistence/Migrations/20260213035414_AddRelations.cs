using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Billing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "Billing",
                table: "MemberTransactions",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "Total",
                schema: "Billing",
                table: "MemberTransactionDetails",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "Price",
                schema: "Billing",
                table: "MemberTransactionDetails",
                newName: "TotalLine");

            migrationBuilder.RenameColumn(
                name: "Amount",
                schema: "Billing",
                table: "MemberTransactionDetails",
                newName: "Quantity");

            migrationBuilder.AddColumn<decimal>(
                name: "Difference",
                schema: "Billing",
                table: "MemberTransactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Received",
                schema: "Billing",
                table: "MemberTransactions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberTransactionDetails_MemberTransactions_IdMemberTransaction",
                schema: "Billing",
                table: "MemberTransactionDetails",
                column: "IdMemberTransaction",
                principalSchema: "Billing",
                principalTable: "MemberTransactions",
                principalColumn: "IdMemberTransaction",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberTransactionDetails_MemberTransactions_IdMemberTransaction",
                schema: "Billing",
                table: "MemberTransactionDetails");

            migrationBuilder.DropColumn(
                name: "Difference",
                schema: "Billing",
                table: "MemberTransactions");

            migrationBuilder.DropColumn(
                name: "Received",
                schema: "Billing",
                table: "MemberTransactions");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                schema: "Billing",
                table: "MemberTransactions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                schema: "Billing",
                table: "MemberTransactionDetails",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "TotalLine",
                schema: "Billing",
                table: "MemberTransactionDetails",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                schema: "Billing",
                table: "MemberTransactionDetails",
                newName: "Amount");
        }
    }
}
