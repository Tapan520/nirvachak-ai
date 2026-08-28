using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidatePartyPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredCandidateId",
                table: "VoterProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredPartyId",
                table: "VoterProfiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SurveyCandidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PartyAffiliation = table.Column<string>(type: "TEXT", nullable: true),
                    PhotoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyCandidates_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyParties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyParties_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoterProfiles_PreferredCandidateId",
                table: "VoterProfiles",
                column: "PreferredCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_VoterProfiles_PreferredPartyId",
                table: "VoterProfiles",
                column: "PreferredPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyCandidates_ConstituencyId",
                table: "SurveyCandidates",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyParties_ConstituencyId",
                table: "SurveyParties",
                column: "ConstituencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_VoterProfiles_SurveyCandidates_PreferredCandidateId",
                table: "VoterProfiles",
                column: "PreferredCandidateId",
                principalTable: "SurveyCandidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_VoterProfiles_SurveyParties_PreferredPartyId",
                table: "VoterProfiles",
                column: "PreferredPartyId",
                principalTable: "SurveyParties",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VoterProfiles_SurveyCandidates_PreferredCandidateId",
                table: "VoterProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_VoterProfiles_SurveyParties_PreferredPartyId",
                table: "VoterProfiles");

            migrationBuilder.DropTable(
                name: "SurveyCandidates");

            migrationBuilder.DropTable(
                name: "SurveyParties");

            migrationBuilder.DropIndex(
                name: "IX_VoterProfiles_PreferredCandidateId",
                table: "VoterProfiles");

            migrationBuilder.DropIndex(
                name: "IX_VoterProfiles_PreferredPartyId",
                table: "VoterProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredCandidateId",
                table: "VoterProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredPartyId",
                table: "VoterProfiles");
        }
    }
}
