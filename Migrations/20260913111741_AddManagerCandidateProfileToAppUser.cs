using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerCandidateProfileToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CandidateName",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CandidatePartyOrSymbol",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CandidatePhotoUrl",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CandidateSlogan",
                table: "AspNetUsers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CandidatePartyOrSymbol",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CandidatePhotoUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CandidateSlogan",
                table: "AspNetUsers");
        }
    }
}
