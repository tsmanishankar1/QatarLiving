using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.QLApplication
{
    /// <inheritdoc />
    public partial class UserLegacyJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LegacyData",
                table: "AspNetUsers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacySubscription",
                table: "AspNetUsers",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegacyData",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LegacySubscription",
                table: "AspNetUsers");
        }
    }
}
