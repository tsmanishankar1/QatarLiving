using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Common.Migrations.QLClassified
{
    /// <inheritdoc />
    public partial class StoreProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.CreateTable(
                name: "StoreProduct",
                columns: table => new
                {
                    StoreProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    ProductLogo = table.Column<string>(type: "text", nullable: false),
                    ProductPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    ProductSummary = table.Column<string>(type: "text", nullable: false),
                    ProductDescription = table.Column<string>(type: "text", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProduct", x => x.StoreProductId);
                });

            migrationBuilder.CreateTable(
                name: "ProductFeature",
                columns: table => new
                {
                    ProductFeaturesId = table.Column<Guid>(type: "uuid", nullable: false),
                    Features = table.Column<string>(type: "text", nullable: false),
                    StoreProductId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFeature", x => x.ProductFeaturesId);
                    table.ForeignKey(
                        name: "FK_ProductFeature_StoreProduct_StoreProductId",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProduct",
                        principalColumn: "StoreProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImage",
                columns: table => new
                {
                    ProductImagesId = table.Column<Guid>(type: "uuid", nullable: false),
                    Images = table.Column<string>(type: "text", nullable: false),
                    StoreProductId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImage", x => x.ProductImagesId);
                    table.ForeignKey(
                        name: "FK_ProductImage_StoreProduct_StoreProductId",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProduct",
                        principalColumn: "StoreProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPageCoordinate",
                columns: table => new
                {
                    PageCoordinatesId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartPixHorizontal = table.Column<int>(type: "integer", nullable: true),
                    StartPixVertical = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    StoreProductId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPageCoordinate", x => x.PageCoordinatesId);
                    table.ForeignKey(
                        name: "FK_ProductPageCoordinate_StoreProduct_StoreProductId",
                        column: x => x.StoreProductId,
                        principalTable: "StoreProduct",
                        principalColumn: "StoreProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFeature_StoreProductId",
                table: "ProductFeature",
                column: "StoreProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImage_StoreProductId",
                table: "ProductImage",
                column: "StoreProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPageCoordinate_StoreProductId",
                table: "ProductPageCoordinate",
                column: "StoreProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductFeature");

            migrationBuilder.DropTable(
                name: "ProductImage");

            migrationBuilder.DropTable(
                name: "ProductPageCoordinate");

            migrationBuilder.DropTable(
                name: "StoreProduct");

           
        }
    }
}
