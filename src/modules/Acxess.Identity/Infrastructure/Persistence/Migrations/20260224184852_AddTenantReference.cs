using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IdTenant",
                schema: "Identity",
                table: "AspNetUsers",
                column: "IdTenant");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_IdTenant",
                schema: "Identity",
                table: "AspNetUsers",
                column: "IdTenant",
                principalSchema: "Identity",
                principalTable: "Tenants",
                principalColumn: "IdTenant",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_IdTenant",
                schema: "Identity",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IdTenant",
                schema: "Identity",
                table: "AspNetUsers");
        }
    }
}
