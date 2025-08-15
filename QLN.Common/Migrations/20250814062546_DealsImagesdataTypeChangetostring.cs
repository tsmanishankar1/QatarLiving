using Microsoft.EntityFrameworkCore.Migrations;
using QLN.Common.DTO_s;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class DealsImagesdataTypeChangetostring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Locations",
                table: "Deal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<LocationsDtos>(
                name: "Locations",
                table: "Deal",
                type: "jsonb",
                nullable: false);
        }
    }
}
