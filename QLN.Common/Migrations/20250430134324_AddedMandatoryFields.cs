using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations
{
    /// <inheritdoc />
    public partial class AddedMandatoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Createdat",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Updatedat",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Createdat",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Updatedat",
                table: "AspNetUsers");
        }
    }
}
