using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class classifiedsDealsSlugAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SubscriptionId",
                table: "Deal",
                type: "uuid",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Deal",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Deal");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionId",
                table: "Deal",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
