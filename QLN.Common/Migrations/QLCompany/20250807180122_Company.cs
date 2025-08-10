using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using QLN.Common.DTO_s;

#nullable disable

namespace QLN.Common.Migrations.QLCompany
{
    /// <inheritdoc />
    public partial class Company : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PhoneNumberCountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BranchLocations = table.Column<List<string>>(type: "jsonb", nullable: true),
                    WhatsAppNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WhatsAppCountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FacebookUrl = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    InstagramUrl = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CompanyLogo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StartDay = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EndDay = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StartHour = table.Column<string>(type: "text", nullable: true),
                    EndHour = table.Column<string>(type: "text", nullable: true),
                    UserDesignation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AuthorisedContactPersonName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CRExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CoverImage1 = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CoverImage2 = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsTherapeuticService = table.Column<bool>(type: "boolean", nullable: true),
                    TherapeuticCertificate = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CompanyType = table.Column<int>(type: "integer", nullable: false),
                    CompanySize = table.Column<int>(type: "integer", nullable: false),
                    NatureOfBusiness = table.Column<List<NatureOfBusiness>>(type: "jsonb", nullable: false),
                    BusinessDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CRNumber = table.Column<int>(type: "integer", nullable: false),
                    CRDocument = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Vertical = table.Column<int>(type: "integer", nullable: false),
                    SubVertical = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsBasicProfile = table.Column<bool>(type: "boolean", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyName",
                table: "Companies",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_UserId",
                table: "Companies",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
