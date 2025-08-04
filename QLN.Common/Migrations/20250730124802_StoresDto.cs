using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations
{
    /// <inheritdoc />
    public partial class StoresDto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "storesDtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    PhoneNumberCountryCode = table.Column<string>(type: "text", nullable: false),
                    BranchLocations = table.Column<List<string>>(type: "text[]", nullable: true),
                    WhatsAppNumber = table.Column<string>(type: "text", nullable: false),
                    WhatsAppCountryCode = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    FacebookUrl = table.Column<string>(type: "text", nullable: true),
                    InstagramUrl = table.Column<string>(type: "text", nullable: true),
                    CompanyLogo = table.Column<string>(type: "text", nullable: false),
                    StartDay = table.Column<string>(type: "text", nullable: true),
                    EndDay = table.Column<string>(type: "text", nullable: true),
                    StartHour = table.Column<TimeSpan>(type: "interval", nullable: true),
                    EndHour = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CompanyType = table.Column<int>(type: "integer", nullable: false),
                    CompanySize = table.Column<int>(type: "integer", nullable: false),
                    NatureOfBusiness = table.Column<int[]>(type: "integer[]", nullable: false),
                    BusinessDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CRNumber = table.Column<int>(type: "integer", nullable: false),
                    CRDocument = table.Column<string>(type: "text", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Vertical = table.Column<int>(type: "integer", nullable: false),
                    SubVertical = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storesDtos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "storesDtos");
        }
    }
}
