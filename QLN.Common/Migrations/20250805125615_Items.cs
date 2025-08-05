using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using QLN.Common.DTO_s;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class Items : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeaturedExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PromotedExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRefreshedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
                    IsRefreshed = table.Column<bool>(type: "boolean", nullable: false),
                    SubVertical = table.Column<int>(type: "integer", nullable: false),
                    AdType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Price = table.Column<double>(type: "double precision", nullable: true),
                    PriceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    L1CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    L1Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    L2CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    L2Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Condition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Color = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    ContactNumberCountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ContactNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WhatsappNumberCountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    WhatsAppNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StreetNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BuildingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Images = table.Column<List<ImageInfo>>(type: "jsonb", nullable: false),
                    Attributes = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Item");
        }
    }
}
