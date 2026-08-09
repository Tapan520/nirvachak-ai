using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ?? Booth Checklist ????????????????????????????????????????????
            migrationBuilder.CreateTable(
                name: "BoothChecklists",
                columns: table => new
                {
                    Id               = table.Column<int>(type: "INTEGER", nullable: false)
                                           .Annotation("Sqlite:Autoincrement", true),
                    BoothNumber      = table.Column<int>(type: "INTEGER", nullable: false),
                    ConstituencyId   = table.Column<int>(type: "INTEGER", nullable: false),
                    AgentPresent     = table.Column<bool>(type: "INTEGER", nullable: false),
                    BannerDisplayed  = table.Column<bool>(type: "INTEGER", nullable: false),
                    VoterListPrinted = table.Column<bool>(type: "INTEGER", nullable: false),
                    TransportArranged= table.Column<bool>(type: "INTEGER", nullable: false),
                    PhoneCharged     = table.Column<bool>(type: "INTEGER", nullable: false),
                    BoothClean       = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes            = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedByUserId= table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedByName  = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt      = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt        = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoothChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoothChecklists_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoothChecklists_ConstituencyId_BoothNumber",
                table: "BoothChecklists",
                columns: new[] { "ConstituencyId", "BoothNumber" },
                unique: true);

            // ?? Voter HouseholdId (for family grouping) ????????????????????
            migrationBuilder.AddColumn<string>(
                name: "HouseholdId",
                table: "Voters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voters_HouseholdId",
                table: "Voters",
                column: "HouseholdId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BoothChecklists");

            migrationBuilder.DropIndex(name: "IX_Voters_HouseholdId", table: "Voters");
            migrationBuilder.DropColumn(name: "HouseholdId", table: "Voters");
        }
    }
}
