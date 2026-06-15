using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventSystem.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLocationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Events",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Events");
        }
    }
}
