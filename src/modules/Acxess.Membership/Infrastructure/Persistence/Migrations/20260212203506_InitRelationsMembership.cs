using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Membership.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitRelationsMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Membership");
            
            migrationBuilder.RenameColumn(
                name: "FirtsName",
                table: "Members",
                newName: "FirstName",
                schema: "Membership");

            migrationBuilder.RenameColumn(
                name: "IsAcive",
                table: "Subscriptions",
                newName: "IsActive",
                schema: "Membership");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionAddOns",
                schema: "Membership");

            migrationBuilder.DropTable(
                name: "SubscriptionMembers",
                schema: "Membership");

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "Membership");

            migrationBuilder.DropTable(
                name: "Members",
                schema: "Membership");
        }
    }
}
