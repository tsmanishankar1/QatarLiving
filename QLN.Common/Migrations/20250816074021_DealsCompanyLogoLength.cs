using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class DealsCompanyLogoLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CompanyLogo",
                table: "Deal",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CompanyLogo",
                table: "Deal",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
