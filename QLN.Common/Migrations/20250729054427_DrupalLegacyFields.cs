using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QLN.Common.Migrations
{
    /// <inheritdoc />
    public partial class DrupalLegacyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "UserSubscription",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "UserCompany",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "LegacyUid",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LegacySubscription",
                columns: table => new
                {
                    ReferenceId = table.Column<string>(type: "text", nullable: false),
                    Uid = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<string>(type: "text", nullable: false),
                    ExpireDate = table.Column<string>(type: "text", nullable: false),
                    ProductType = table.Column<string>(type: "text", nullable: false),
                    AccessDashboard = table.Column<string>(type: "text", nullable: true),
                    ProductClass = table.Column<string>(type: "text", nullable: false),
                    Categories = table.Column<List<string>>(type: "text[]", nullable: true),
                    SubscriptionCategories = table.Column<List<string>>(type: "text[]", nullable: true),
                    Snid = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacySubscription", x => x.ReferenceId);
                });

            migrationBuilder.CreateTable(
                name: "UserLegacyData",
                columns: table => new
                {
                    Uid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<string>(type: "text", nullable: false),
                    Access = table.Column<string>(type: "text", nullable: false),
                    Init = table.Column<string>(type: "text", nullable: false),
                    QlnextUserId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Alias = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: true),
                    Image = table.Column<string>(type: "text", nullable: true),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions = table.Column<List<string>>(type: "text[]", nullable: true),
                    Roles = table.Column<List<string>>(type: "text[]", nullable: true),
                    SubscriptionReferenceId = table.Column<string>(type: "text", nullable: true),
                    ApplicationUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLegacyData", x => x.Uid);
                    table.ForeignKey(
                        name: "FK_UserLegacyData_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserLegacyData_LegacySubscription_SubscriptionReferenceId",
                        column: x => x.SubscriptionReferenceId,
                        principalTable: "LegacySubscription",
                        principalColumn: "ReferenceId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LegacyUid",
                table: "AspNetUsers",
                column: "LegacyUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubscription_Uid",
                table: "LegacySubscription",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLegacyData_ApplicationUserId",
                table: "UserLegacyData",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegacyData_SubscriptionReferenceId",
                table: "UserLegacyData",
                column: "SubscriptionReferenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserLegacyData_LegacyUid",
                table: "AspNetUsers",
                column: "LegacyUid",
                principalTable: "UserLegacyData",
                principalColumn: "Uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LegacySubscription_UserLegacyData_Uid",
                table: "LegacySubscription",
                column: "Uid",
                principalTable: "UserLegacyData",
                principalColumn: "Uid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserLegacyData_LegacyUid",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_LegacySubscription_UserLegacyData_Uid",
                table: "LegacySubscription");

            migrationBuilder.DropTable(
                name: "UserLegacyData");

            migrationBuilder.DropTable(
                name: "LegacySubscription");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LegacyUid",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserSubscription");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserCompany");

            migrationBuilder.DropColumn(
                name: "LegacyUid",
                table: "AspNetUsers");
        }
    }
}
