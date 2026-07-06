using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddElectionEnhancementModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoothShiftAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VolunteerId = table.Column<int>(type: "INTEGER", nullable: false),
                    BoothNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ShiftStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ShiftEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoothShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoothShiftAssignments_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoothShiftAssignments_Volunteers_VolunteerId",
                        column: x => x.VolunteerId,
                        principalTable: "Volunteers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    PlannedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetPlans_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElectionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoothNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CandidateVotes = table.Column<int>(type: "INTEGER", nullable: false),
                    Competitor1Votes = table.Column<int>(type: "INTEGER", nullable: true),
                    Competitor1Name = table.Column<string>(type: "TEXT", nullable: true),
                    Competitor2Votes = table.Column<int>(type: "INTEGER", nullable: true),
                    Competitor2Name = table.Column<string>(type: "TEXT", nullable: true),
                    Competitor3Votes = table.Column<int>(type: "INTEGER", nullable: true),
                    Competitor3Name = table.Column<string>(type: "TEXT", nullable: true),
                    TotalVotesCast = table.Column<int>(type: "INTEGER", nullable: true),
                    IsFinal = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    EnteredByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    EnteredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectionResults_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FieldReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkerUserId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkerName = table.Column<string>(type: "TEXT", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ContactsMade = table.Column<int>(type: "INTEGER", nullable: false),
                    FavourContacts = table.Column<int>(type: "INTEGER", nullable: false),
                    FloatingContacts = table.Column<int>(type: "INTEGER", nullable: false),
                    AgainstContacts = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuesLogged = table.Column<int>(type: "INTEGER", nullable: false),
                    Highlights = table.Column<string>(type: "TEXT", nullable: true),
                    Challenges = table.Column<string>(type: "TEXT", nullable: true),
                    PlannedForTomorrow = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewerNotes = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldReports_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageTemplates_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PannaPramukhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    BoothNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    PannaNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalVotersAssigned = table.Column<int>(type: "INTEGER", nullable: false),
                    VotersContacted = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PannaPramukhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PannaPramukhs_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RapidResponseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    AffectedWards = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedToUserId = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedToName = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseText = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ThreatLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoggedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RapidResponseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RapidResponseItems_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportVehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DriverName = table.Column<string>(type: "TEXT", nullable: false),
                    DriverPhone = table.Column<string>(type: "TEXT", nullable: false),
                    VehicleNumber = table.Column<string>(type: "TEXT", nullable: true),
                    VehicleType = table.Column<string>(type: "TEXT", nullable: true),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    BoothNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportVehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportVehicles_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoterTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: false),
                    AddedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoterTags_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageBroadcasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TemplateId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetFilter = table.Column<string>(type: "TEXT", nullable: true),
                    TargetDescription = table.Column<string>(type: "TEXT", nullable: true),
                    TotalTargeted = table.Column<int>(type: "INTEGER", nullable: false),
                    SentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedByName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageBroadcasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageBroadcasts_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageBroadcasts_MessageTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "MessageTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoterTransportRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoterId = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PickupAddress = table.Column<string>(type: "TEXT", nullable: true),
                    PickupNotes = table.Column<string>(type: "TEXT", nullable: true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PickedUpAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterTransportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoterTransportRequests_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoterTransportRequests_TransportVehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "TransportVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VoterTransportRequests_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoothShiftAssignments_ConstituencyId",
                table: "BoothShiftAssignments",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BoothShiftAssignments_VolunteerId",
                table: "BoothShiftAssignments",
                column: "VolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlans_ConstituencyId",
                table: "BudgetPlans",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectionResults_ConstituencyId",
                table: "ElectionResults",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldReports_ConstituencyId",
                table: "FieldReports",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageBroadcasts_ConstituencyId",
                table: "MessageBroadcasts",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageBroadcasts_TemplateId",
                table: "MessageBroadcasts",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_ConstituencyId",
                table: "MessageTemplates",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PannaPramukhs_ConstituencyId",
                table: "PannaPramukhs",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_RapidResponseItems_ConstituencyId",
                table: "RapidResponseItems",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportVehicles_ConstituencyId",
                table: "TransportVehicles",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_VoterTags_VoterId_Tag",
                table: "VoterTags",
                columns: new[] { "VoterId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoterTransportRequests_ConstituencyId",
                table: "VoterTransportRequests",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_VoterTransportRequests_VehicleId",
                table: "VoterTransportRequests",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VoterTransportRequests_VoterId",
                table: "VoterTransportRequests",
                column: "VoterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoothShiftAssignments");

            migrationBuilder.DropTable(
                name: "BudgetPlans");

            migrationBuilder.DropTable(
                name: "ElectionResults");

            migrationBuilder.DropTable(
                name: "FieldReports");

            migrationBuilder.DropTable(
                name: "MessageBroadcasts");

            migrationBuilder.DropTable(
                name: "PannaPramukhs");

            migrationBuilder.DropTable(
                name: "RapidResponseItems");

            migrationBuilder.DropTable(
                name: "VoterTags");

            migrationBuilder.DropTable(
                name: "VoterTransportRequests");

            migrationBuilder.DropTable(
                name: "MessageTemplates");

            migrationBuilder.DropTable(
                name: "TransportVehicles");
        }
    }
}
