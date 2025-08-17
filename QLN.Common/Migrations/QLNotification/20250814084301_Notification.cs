using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using QLN.Common.DTO_s;

#nullable disable

namespace QLN.Common.Migrations.QLNotification
{
    /// <inheritdoc />
    public partial class Notification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttachmentDto",
                columns: table => new
                {
                    Filename = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Destinations = table.Column<List<string>>(type: "jsonb", nullable: false),
                    Sender = table.Column<SenderDto>(type: "jsonb", nullable: true),
                    Recipients = table.Column<List<RecipientDto>>(type: "jsonb", nullable: false),
                    Subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Plaintext = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Html = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    Attachments = table.Column<List<AttachmentDto>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttachmentDto");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
