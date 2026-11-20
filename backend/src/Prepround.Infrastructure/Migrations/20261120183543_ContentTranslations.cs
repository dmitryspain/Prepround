using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prepround.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ContentTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionTranslations",
                table: "Topics",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "TitleTranslations",
                table: "Topics",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "TitleTranslations",
                table: "Stacks",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "TitleTranslations",
                table: "Levels",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "TitleTranslations",
                table: "Fields",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "TitleTranslations",
                table: "Directions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateTable(
                name: "QuestionTranslations",
                columns: table => new
                {
                    QuestionId = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    OptionTexts = table.Column<string>(type: "jsonb", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTranslations", x => new { x.QuestionId, x.Language });
                    table.ForeignKey(
                        name: "FK_QuestionTranslations_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionTranslations");

            migrationBuilder.DropColumn(
                name: "DescriptionTranslations",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "TitleTranslations",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "TitleTranslations",
                table: "Stacks");

            migrationBuilder.DropColumn(
                name: "TitleTranslations",
                table: "Levels");

            migrationBuilder.DropColumn(
                name: "TitleTranslations",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "TitleTranslations",
                table: "Directions");
        }
    }
}
