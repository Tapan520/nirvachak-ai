using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitorInfluencerPhoneBanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompetitorActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompetitorName = table.Column<string>(type: "TEXT", nullable: false),
                    PartyName = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityTitle = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityType = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Ward = table.Column<string>(type: "TEXT", nullable: true),
                    BoothNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    ActivityDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EstimatedCrowd = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ThreatLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoggedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorActivities_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Influencers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    MobileNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Community = table.Column<string>(type: "TEXT", nullable: true),
                    EstimatedFollowers = table.Column<int>(type: "INTEGER", nullable: true),
                    Ward = table.Column<string>(type: "TEXT", nullable: true),
                    BoothNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Alignment = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    LastMetAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastMeetingOutcome = table.Column<string>(type: "TEXT", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Influencers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Influencers_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneCallLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoterId = table.Column<int>(type: "INTEGER", nullable: false),
                    CalledByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CalledByName = table.Column<string>(type: "TEXT", nullable: true),
                    CalledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    SentimentAfterCall = table.Column<int>(type: "INTEGER", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneCallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneCallLogs_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhoneCallLogs_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorActivities_ConstituencyId",
                table: "CompetitorActivities",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Influencers_ConstituencyId",
                table: "Influencers",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCallLogs_ConstituencyId",
                table: "PhoneCallLogs",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneCallLogs_VoterId",
                table: "PhoneCallLogs",
                column: "VoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitorActivities");

            migrationBuilder.DropTable(
                name: "Influencers");

            migrationBuilder.DropTable(
                name: "PhoneCallLogs");
        }
    }
}
