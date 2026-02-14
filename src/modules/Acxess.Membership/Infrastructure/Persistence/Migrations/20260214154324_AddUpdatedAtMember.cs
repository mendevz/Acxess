using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Membership.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Membership",
                table: "Members",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "Membership",
                table: "Members");
        }
    }
}
