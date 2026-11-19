using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prepround.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fields",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Directions",
                columns: table => new
                {
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Directions", x => new { x.FieldId, x.Id });
                    table.ForeignKey(
                        name: "FK_Directions_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Levels",
                columns: table => new
                {
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Levels", x => new { x.FieldId, x.Id });
                    table.ForeignKey(
                        name: "FK_Levels_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => new { x.FieldId, x.Id });
                    table.ForeignKey(
                        name: "FK_Topics_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stacks",
                columns: table => new
                {
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: false),
                    DirectionId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stacks", x => new { x.FieldId, x.Id });
                    table.ForeignKey(
                        name: "FK_Stacks_Directions_FieldId_DirectionId",
                        columns: x => new { x.FieldId, x.DirectionId },
                        principalTable: "Directions",
                        principalColumns: new[] { "FieldId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    TopicId = table.Column<string>(type: "text", nullable: false),
                    LevelId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CodeLang = table.Column<string>(type: "text", nullable: true),
                    CodeSource = table.Column<string>(type: "text", nullable: true),
                    Options = table.Column<string>(type: "jsonb", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Levels_FieldId_LevelId",
                        columns: x => new { x.FieldId, x.LevelId },
                        principalTable: "Levels",
                        principalColumns: new[] { "FieldId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_Topics_FieldId_TopicId",
                        columns: x => new { x.FieldId, x.TopicId },
                        principalTable: "Topics",
                        principalColumns: new[] { "FieldId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StackTopics",
                columns: table => new
                {
                    FieldId = table.Column<string>(type: "text", nullable: false),
                    StackId = table.Column<string>(type: "text", nullable: false),
                    TopicId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StackTopics", x => new { x.FieldId, x.StackId, x.TopicId });
                    table.ForeignKey(
                        name: "FK_StackTopics_Stacks_FieldId_StackId",
                        columns: x => new { x.FieldId, x.StackId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "FieldId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StackTopics_Topics_FieldId_TopicId",
                        columns: x => new { x.FieldId, x.TopicId },
                        principalTable: "Topics",
                        principalColumns: new[] { "FieldId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_FieldId_LevelId",
                table: "Questions",
                columns: new[] { "FieldId", "LevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_FieldId_TopicId",
                table: "Questions",
                columns: new[] { "FieldId", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_Stacks_FieldId_DirectionId",
                table: "Stacks",
                columns: new[] { "FieldId", "DirectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StackTopics_FieldId_TopicId",
                table: "StackTopics",
                columns: new[] { "FieldId", "TopicId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "StackTopics");

            migrationBuilder.DropTable(
                name: "Levels");

            migrationBuilder.DropTable(
                name: "Stacks");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "Directions");

            migrationBuilder.DropTable(
                name: "Fields");
        }
    }
}
