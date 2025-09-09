using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using QLN.Common.DTO_s;

#nullable disable

namespace QLN.Common.Migrations.ClassifiedDev
{
    /// <inheritdoc />
    public partial class SeasonalPicksDealsv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
     name: "Deal",
     columns: table => new
     {
         Id = table.Column<long>(type: "bigint", nullable: false)
             .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
         SubscriptionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
         UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
         BusinessName = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
         BranchNames = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
         BusinessType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
         Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
         StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
         EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
         FlyerFileUrl = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
         DataFeedUrl = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
         ContactNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
         WhatsappNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
         WebsiteUrl = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
         SocialMediaLinks = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
         IsActive = table.Column<bool>(type: "boolean", nullable: false),
         Locations = table.Column<LocationDto>(type: "jsonb", nullable: false),
         CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
         CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
         UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
         UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
         XMLlink = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
         Offertitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
         Images = table.Column<List<ImageInfo>>(type: "jsonb", nullable: false),
         ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
         FeaturedExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
         PromotedExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
         IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
         IsPromoted = table.Column<bool>(type: "boolean", nullable: false),
         Status = table.Column<int>(type: "integer", nullable: false)
     },
     constraints: table =>
     {
         table.PrimaryKey("PK_Deal", x => x.Id);
     });

            migrationBuilder.CreateTable(
                name: "SeasonalPicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Vertical = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    L1CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    L1categoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    L2categoryId = table.Column<long>(type: "bigint", nullable: false),
                    L2categoryName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SlotOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalPicks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deal");

            migrationBuilder.DropTable(
                name: "SeasonalPicks");
        }
    }
}
