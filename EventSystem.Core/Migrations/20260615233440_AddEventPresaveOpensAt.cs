using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventSystem.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPresaveOpensAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PresaveOpensAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PresaveOpensAt",
                table: "Events");
        }
    }
}
