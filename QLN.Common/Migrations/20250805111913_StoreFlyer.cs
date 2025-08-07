using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.QLClassified
{
    /// <inheritdoc />
    public partial class StoreFlyer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FlyerId",
                table: "StoreProduct",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "StoreFlyer",
                columns: table => new
                {
                    StoreFlyersId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreFlyer", x => x.StoreFlyersId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreProduct_FlyerId",
                table: "StoreProduct",
                column: "FlyerId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoreProduct_StoreFlyer_FlyerId",
                table: "StoreProduct",
                column: "FlyerId",
                principalTable: "StoreFlyer",
                principalColumn: "StoreFlyersId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoreProduct_StoreFlyer_FlyerId",
                table: "StoreProduct");

            migrationBuilder.DropTable(
                name: "StoreFlyer");

            migrationBuilder.DropIndex(
                name: "IX_StoreProduct_FlyerId",
                table: "StoreProduct");

            migrationBuilder.DropColumn(
                name: "FlyerId",
                table: "StoreProduct");
        }
    }
}
