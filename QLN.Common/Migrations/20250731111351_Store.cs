using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QLN.Common.Migrations
{
    /// <inheritdoc />
    public partial class Store : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Store",
                columns: table => new
                {
                    StoresID = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Designation = table.Column<string>(type: "text", nullable: false),
                    WhatsAppNumber = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    Website = table.Column<string>(type: "text", nullable: false),
                    Facebook = table.Column<string>(type: "text", nullable: false),
                    Instagram = table.Column<string>(type: "text", nullable: false),
                    Logo = table.Column<string>(type: "text", nullable: false),
                    Banner = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OrderID = table.Column<int>(type: "integer", nullable: false),
                    StoreStatusId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Store", x => x.StoresID);
                });

            migrationBuilder.CreateTable(
                name: "StoreAddress",
                columns: table => new
                {
                    StoreAddressId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Address = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUser = table.Column<string>(type: "text", nullable: false),
                    UpdatedUser = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<bool>(type: "boolean", nullable: false),
                    StoresID = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreAddress", x => x.StoreAddressId);
                    table.ForeignKey(
                        name: "FK_StoreAddress_Store_StoresID",
                        column: x => x.StoresID,
                        principalTable: "Store",
                        principalColumn: "StoresID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreDocuments",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Document = table.Column<string>(type: "text", nullable: false),
                    DocumentURL = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUser = table.Column<string>(type: "text", nullable: false),
                    UpdatedUser = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<bool>(type: "boolean", nullable: false),
                    StoresID = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreDocuments", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_StoreDocuments_Store_StoresID",
                        column: x => x.StoresID,
                        principalTable: "Store",
                        principalColumn: "StoresID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoreLicense",
                columns: table => new
                {
                    StoreLicenseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CRDocument = table.Column<string>(type: "text", nullable: false),
                    CRDocumentURL = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUser = table.Column<string>(type: "text", nullable: false),
                    UpdatedUser = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<bool>(type: "boolean", nullable: false),
                    StoresID = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreLicense", x => x.StoreLicenseId);
                    table.ForeignKey(
                        name: "FK_StoreLicense_Store_StoresID",
                        column: x => x.StoresID,
                        principalTable: "Store",
                        principalColumn: "StoresID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreAddress_StoresID",
                table: "StoreAddress",
                column: "StoresID");

            migrationBuilder.CreateIndex(
                name: "IX_StoreDocuments_StoresID",
                table: "StoreDocuments",
                column: "StoresID");

            migrationBuilder.CreateIndex(
                name: "IX_StoreLicense_StoresID",
                table: "StoreLicense",
                column: "StoresID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreAddress");

            migrationBuilder.DropTable(
                name: "StoreDocuments");

            migrationBuilder.DropTable(
                name: "StoreLicense");

            migrationBuilder.DropTable(
                name: "Store");
        }
    }
}
