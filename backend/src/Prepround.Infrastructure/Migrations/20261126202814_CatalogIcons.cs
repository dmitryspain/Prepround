using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prepround.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogIcons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Topics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Stacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "Stacks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Stacks");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "Stacks");
        }
    }
}
