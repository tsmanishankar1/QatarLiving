using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.QLApplication
{
    /// <inheritdoc />
    public partial class updateLegacyDrupalToNotAList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserLegacyData_AspNetUsers_ApplicationUserId",
                table: "UserLegacyData");

            migrationBuilder.DropIndex(
                name: "IX_UserLegacyData_ApplicationUserId",
                table: "UserLegacyData");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "UserLegacyData");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompany",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "LegacyDataUid",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LegacyDataUid",
                table: "AspNetUsers",
                column: "LegacyDataUid");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserLegacyData_LegacyDataUid",
                table: "AspNetUsers",
                column: "LegacyDataUid",
                principalTable: "UserLegacyData",
                principalColumn: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserLegacyData_LegacyDataUid",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LegacyDataUid",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsCompany",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LegacyDataUid",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "UserLegacyData",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLegacyData_ApplicationUserId",
                table: "UserLegacyData",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserLegacyData_AspNetUsers_ApplicationUserId",
                table: "UserLegacyData",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
