using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LunaArcSync.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedOcrColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OcrDataNormalized",
                table: "Versions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcrDataNormalized",
                table: "Versions");
        }
    }
}
