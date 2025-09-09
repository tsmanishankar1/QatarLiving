using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class OnholdFieldforClassifieds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Onhold",
                table: "Preloved",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Onhold",
                table: "Item",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Onhold",
                table: "Collectible",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Onhold",
                table: "Preloved");

            migrationBuilder.DropColumn(
                name: "Onhold",
                table: "Item");

            migrationBuilder.DropColumn(
                name: "Onhold",
                table: "Collectible");
        }
    }
}
