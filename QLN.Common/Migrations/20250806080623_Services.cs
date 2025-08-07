using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using QLN.Common.DTO_s;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class Services : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServicesCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicesCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "L1Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ServicesCategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L1Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_L1Categories_ServicesCategories_ServicesCategoryId",
                        column: x => x.ServicesCategoryId,
                        principalTable: "ServicesCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "L2Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    L1CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L2Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_L2Categories_L1Categories_L1CategoryId",
                        column: x => x.L1CategoryId,
                        principalTable: "L1Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    L1CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    L2CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryName = table.Column<string>(type: "text", nullable: true),
                    L1CategoryName = table.Column<string>(type: "text", nullable: true),
                    L2CategoryName = table.Column<string>(type: "text", nullable: true),
                    IsPriceOnRequest = table.Column<bool>(type: "boolean", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PhoneNumberCountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    WhatsappNumberCountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    WhatsappNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: true),
                    SubscriptionId = table.Column<string>(type: "text", nullable: true),
                    ZoneId = table.Column<string>(type: "text", nullable: false),
                    StreetNumber = table.Column<string>(type: "text", nullable: true),
                    BuildingNumber = table.Column<string>(type: "text", nullable: true),
                    LicenseCertificate = table.Column<string>(type: "text", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    Lattitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    PhotoUpload = table.Column<List<ImageDto>>(type: "jsonb", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
                    PromotedExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FeaturedExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRefreshedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdType = table.Column<int>(type: "integer", nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_L1Categories_L1CategoryId",
                        column: x => x.L1CategoryId,
                        principalTable: "L1Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Services_L2Categories_L2CategoryId",
                        column: x => x.L2CategoryId,
                        principalTable: "L2Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Services_ServicesCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ServicesCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_L1Categories_ServicesCategoryId",
                table: "L1Categories",
                column: "ServicesCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_L2Categories_L1CategoryId",
                table: "L2Categories",
                column: "L1CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryId",
                table: "Services",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_L1CategoryId",
                table: "Services",
                column: "L1CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_L2CategoryId",
                table: "Services",
                column: "L2CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "L2Categories");

            migrationBuilder.DropTable(
                name: "L1Categories");

            migrationBuilder.DropTable(
                name: "ServicesCategories");
        }
    }
}
