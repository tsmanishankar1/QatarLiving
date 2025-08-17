using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.QLApplication
{
    /// <inheritdoc />
    public partial class UserSubscriptionUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "UserSubscription",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "UserSubscription",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "UserSubscription",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "UserSubscription",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "SubVertical",
                table: "UserSubscription",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Vertical",
                table: "UserSubscription",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "UserSubscription");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "UserSubscription");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "UserSubscription");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "UserSubscription");

            migrationBuilder.DropColumn(
                name: "SubVertical",
                table: "UserSubscription");

            migrationBuilder.DropColumn(
                name: "Vertical",
                table: "UserSubscription");
        }
    }
}
