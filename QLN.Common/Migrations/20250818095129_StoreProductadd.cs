using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class StoreProductadd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "StoresDashboardSummaryItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "StoresDashboardSummaryItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "StoresDashboardHeaderItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "StoresDashboardHeaderItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "CompanyVerificationStatus",
                table: "StoresDashboardHeaderItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "PageNumber",
                table: "StoreProduct",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "StoreProduct",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductBarcode",
                table: "StoreProduct",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Qty",
                table: "StoreProduct",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FlyerId",
                table: "StoreFlyer",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "StoreFlyer",
                type: "text",
                nullable: true);

            

            migrationBuilder.CreateTable(
                name: "StoreSubscriptionQuotaDtos",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quota = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreSubscriptionQuotaDtos");

            migrationBuilder.DropColumn(
                name: "CompanyVerificationStatus",
                table: "StoresDashboardHeaderItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "StoreProduct");

            migrationBuilder.DropColumn(
                name: "ProductBarcode",
                table: "StoreProduct");

            migrationBuilder.DropColumn(
                name: "Qty",
                table: "StoreProduct");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "StoreFlyer");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "FeaturedStores");

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "StoresDashboardSummaryItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "StoresDashboardSummaryItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "StoresDashboardHeaderItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "StoresDashboardHeaderItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PageNumber",
                table: "StoreProduct",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FlyerId",
                table: "StoreFlyer",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
