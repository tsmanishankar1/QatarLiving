using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class categorydropdowns1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryDropdowns_Categories_ParentId",
                table: "CategoryDropdowns");

            migrationBuilder.DropIndex(
                name: "IX_CategoryDropdowns_ParentId",
                table: "CategoryDropdowns");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "CategoryDropdowns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentId",
                table: "CategoryDropdowns",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryDropdowns_ParentId",
                table: "CategoryDropdowns",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryDropdowns_Categories_ParentId",
                table: "CategoryDropdowns",
                column: "ParentId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
