using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyCandidateDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "SurveyCandidates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "SurveyCandidates");
        }
    }
}
