using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLN.Classified.MS.Migrations
{
    /// <inheritdoc />
    public partial class StoresSubscriptionDto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoresSubscriptions",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionType = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Mobile = table.Column<string>(type: "text", nullable: true),
                    Whatsapp = table.Column<string>(type: "text", nullable: true),
                    WebUrl = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WebLeads = table.Column<int>(type: "integer", nullable: false),
                    EmailLeads = table.Column<int>(type: "integer", nullable: false),
                    WhatsappLeads = table.Column<int>(type: "integer", nullable: false),
                    PhoneLeads = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoresSubscriptions");
        }
    }
}
