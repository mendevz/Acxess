using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Membership.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitRelationsMembershipv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTenant",
                schema: "Membership",
                table: "SubscriptionMembers",
                type: "int",
                nullable: false,
                defaultValue: 0); // Valor temporal para filas existentes

            migrationBuilder.AddColumn<int>(
                name: "IdTenant",
                schema: "Membership",
                table: "SubscriptionAddOns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Crear índices para optimizar el filtro global
            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionMembers_IdTenant",
                schema: "Membership",
                table: "SubscriptionMembers",
                column: "IdTenant");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAddOns_IdTenant",
                schema: "Membership",
                table: "SubscriptionAddOns",
                column: "IdTenant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdTenant",
                schema: "Membership",
                table: "SubscriptionMembers");
            
            migrationBuilder.DropColumn(
                name: "IdTenant",
                schema: "Membership",
                table: "SubscriptionAddOns");
        }
    }
}
