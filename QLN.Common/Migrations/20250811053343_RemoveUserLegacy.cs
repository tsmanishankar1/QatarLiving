using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QLN.Common.Migrations.QLApplication
{
    /// <inheritdoc />
    public partial class RemoveUserLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserLegacyData_LegacyDataUid",
                table: "AspNetUsers");

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
                name: "IX_AspNetUsers_LegacyDataUid",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LegacyUid",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LegacyDataUid",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LegacyDataUid",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LegacySubscription",
                columns: table => new
                {
                    Uid = table.Column<long>(type: "bigint", nullable: false),
                    AccessDashboard = table.Column<string>(type: "text", nullable: true),
                    Categories = table.Column<List<string>>(type: "text[]", nullable: true),
                    ExpireDate = table.Column<string>(type: "text", nullable: false),
                    ProductClass = table.Column<string>(type: "text", nullable: false),
                    ProductType = table.Column<string>(type: "text", nullable: false),
                    ReferenceId = table.Column<string>(type: "text", nullable: false),
                    Snid = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubscriptionCategories = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacySubscription", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "UserLegacyData",
                columns: table => new
                {
                    Uid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubscriptionUid = table.Column<long>(type: "bigint", nullable: true),
                    Access = table.Column<string>(type: "text", nullable: false),
                    Alias = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Image = table.Column<string>(type: "text", nullable: true),
                    Init = table.Column<string>(type: "text", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Path = table.Column<string>(type: "text", nullable: true),
                    Permissions = table.Column<List<string>>(type: "text[]", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    QlnextUserId = table.Column<string>(type: "text", nullable: true),
                    Roles = table.Column<List<string>>(type: "text[]", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLegacyData", x => x.Uid);
                    table.ForeignKey(
                        name: "FK_UserLegacyData_LegacySubscription_SubscriptionUid",
                        column: x => x.SubscriptionUid,
                        principalTable: "LegacySubscription",
                        principalColumn: "Uid");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LegacyDataUid",
                table: "AspNetUsers",
                column: "LegacyDataUid");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LegacyUid",
                table: "AspNetUsers",
                column: "LegacyUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLegacyData_SubscriptionUid",
                table: "UserLegacyData",
                column: "SubscriptionUid");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserLegacyData_LegacyDataUid",
                table: "AspNetUsers",
                column: "LegacyDataUid",
                principalTable: "UserLegacyData",
                principalColumn: "Uid");

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
    }
}
