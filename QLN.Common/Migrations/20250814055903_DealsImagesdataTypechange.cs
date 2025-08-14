using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using QLN.Common.DTO_s;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class DealsImagesdataTypechange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Images",
                table: "Deal");

            migrationBuilder.AddColumn<string>(
                name: "CoverImage",
                table: "Deal",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImage",
                table: "Deal");

            migrationBuilder.AddColumn<List<ImageInfo>>(
                name: "Images",
                table: "Deal",
                type: "jsonb",
                nullable: false);
        }
    }
}
