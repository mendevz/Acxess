using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acxess.Marketing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationsPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTenant",
                schema: "Marketing",
                table: "AppliedPromotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_IdPromotion",
                schema: "Marketing",
                table: "Coupons",
                column: "IdPromotion");

            migrationBuilder.CreateIndex(
                name: "IX_AppliedPromotions_IdCoupon",
                schema: "Marketing",
                table: "AppliedPromotions",
                column: "IdCoupon");

            migrationBuilder.CreateIndex(
                name: "IX_AppliedPromotions_IdPromotion",
                schema: "Marketing",
                table: "AppliedPromotions",
                column: "IdPromotion");

            migrationBuilder.AddForeignKey(
                name: "FK_AppliedPromotions_Coupons_IdCoupon",
                schema: "Marketing",
                table: "AppliedPromotions",
                column: "IdCoupon",
                principalSchema: "Marketing",
                principalTable: "Coupons",
                principalColumn: "IdCoupon");

            migrationBuilder.AddForeignKey(
                name: "FK_AppliedPromotions_Promotions_IdPromotion",
                schema: "Marketing",
                table: "AppliedPromotions",
                column: "IdPromotion",
                principalSchema: "Marketing",
                principalTable: "Promotions",
                principalColumn: "IdPromotion");

            migrationBuilder.AddForeignKey(
                name: "FK_Coupons_Promotions_IdPromotion",
                schema: "Marketing",
                table: "Coupons",
                column: "IdPromotion",
                principalSchema: "Marketing",
                principalTable: "Promotions",
                principalColumn: "IdPromotion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppliedPromotions_Coupons_IdCoupon",
                schema: "Marketing",
                table: "AppliedPromotions");

            migrationBuilder.DropForeignKey(
                name: "FK_AppliedPromotions_Promotions_IdPromotion",
                schema: "Marketing",
                table: "AppliedPromotions");

            migrationBuilder.DropForeignKey(
                name: "FK_Coupons_Promotions_IdPromotion",
                schema: "Marketing",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_IdPromotion",
                schema: "Marketing",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_AppliedPromotions_IdCoupon",
                schema: "Marketing",
                table: "AppliedPromotions");

            migrationBuilder.DropIndex(
                name: "IX_AppliedPromotions_IdPromotion",
                schema: "Marketing",
                table: "AppliedPromotions");

            migrationBuilder.DropColumn(
                name: "IdTenant",
                schema: "Marketing",
                table: "AppliedPromotions");
        }
    }
}
