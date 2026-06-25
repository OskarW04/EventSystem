using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventSystem.Core.Migrations
{
    /// <inheritdoc />
    public partial class ChangePresaveNotifiedToTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notified",
                table: "EventPresaves");

            migrationBuilder.AddColumn<DateTime>(
                name: "NotifiedAt",
                table: "EventPresaves",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifiedAt",
                table: "EventPresaves");

            migrationBuilder.AddColumn<bool>(
                name: "Notified",
                table: "EventPresaves",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
