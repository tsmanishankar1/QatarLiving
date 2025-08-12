using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.QLApplication
{
    /// <inheritdoc />
    public partial class FixLegacySubscriptionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserLegacyData_LegacySubscription_SubscriptionReferenceId",
                table: "UserLegacyData");

            migrationBuilder.DropIndex(
                name: "IX_UserLegacyData_SubscriptionReferenceId",
                table: "UserLegacyData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LegacySubscription",
                table: "LegacySubscription");

            migrationBuilder.DropIndex(
                name: "IX_LegacySubscription_Uid",
                table: "LegacySubscription");

            migrationBuilder.DropColumn(
                name: "SubscriptionReferenceId",
                table: "UserLegacyData");

            migrationBuilder.AddColumn<long>(
                name: "SubscriptionUid",
                table: "UserLegacyData",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LegacySubscription",
                table: "LegacySubscription",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegacyData_SubscriptionUid",
                table: "UserLegacyData",
                column: "SubscriptionUid");

            migrationBuilder.AddForeignKey(
                name: "FK_UserLegacyData_LegacySubscription_SubscriptionUid",
                table: "UserLegacyData",
                column: "SubscriptionUid",
                principalTable: "LegacySubscription",
                principalColumn: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserLegacyData_LegacySubscription_SubscriptionUid",
                table: "UserLegacyData");

            migrationBuilder.DropIndex(
                name: "IX_UserLegacyData_SubscriptionUid",
                table: "UserLegacyData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LegacySubscription",
                table: "LegacySubscription");

            migrationBuilder.DropColumn(
                name: "SubscriptionUid",
                table: "UserLegacyData");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionReferenceId",
                table: "UserLegacyData",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_LegacySubscription",
                table: "LegacySubscription",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLegacyData_SubscriptionReferenceId",
                table: "UserLegacyData",
                column: "SubscriptionReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacySubscription_Uid",
                table: "LegacySubscription",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserLegacyData_LegacySubscription_SubscriptionReferenceId",
                table: "UserLegacyData",
                column: "SubscriptionReferenceId",
                principalTable: "LegacySubscription",
                principalColumn: "ReferenceId");
        }
    }
}
