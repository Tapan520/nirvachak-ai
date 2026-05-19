using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddVoterSelfSurveyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RewardConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    PartnerBrand = table.Column<string>(type: "TEXT", nullable: true),
                    CouponCodePrefix = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardConfigs_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoterConsents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoterId = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowCampaignOutreach = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowWhatsAppMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowSchemeNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowDataForAnalytics = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConsentGivenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoterConsents_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoterProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoterId = table.Column<int>(type: "INTEGER", nullable: false),
                    AgeBracket = table.Column<string>(type: "TEXT", nullable: true),
                    CasteCategory = table.Column<string>(type: "TEXT", nullable: true),
                    Religion = table.Column<string>(type: "TEXT", nullable: true),
                    Education = table.Column<string>(type: "TEXT", nullable: true),
                    Occupation = table.Column<string>(type: "TEXT", nullable: true),
                    MonthlyIncomeBracket = table.Column<string>(type: "TEXT", nullable: true),
                    PrimaryConcerns = table.Column<string>(type: "TEXT", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoterProfiles_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CouponPools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RewardConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    CouponCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsIssued = table.Column<bool>(type: "INTEGER", nullable: false),
                    IssuedToVoterId = table.Column<int>(type: "INTEGER", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsRedeemed = table.Column<bool>(type: "INTEGER", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponPools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CouponPools_RewardConfigs_RewardConfigId",
                        column: x => x.RewardConfigId,
                        principalTable: "RewardConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CouponPools_Voters_IssuedToVoterId",
                        column: x => x.IssuedToVoterId,
                        principalTable: "Voters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SurveyCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VoterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CouponId = table.Column<int>(type: "INTEGER", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyCompletions_CouponPools_CouponId",
                        column: x => x.CouponId,
                        principalTable: "CouponPools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SurveyCompletions_Voters_VoterId",
                        column: x => x.VoterId,
                        principalTable: "Voters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CouponPools_CouponCode",
                table: "CouponPools",
                column: "CouponCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponPools_IssuedToVoterId",
                table: "CouponPools",
                column: "IssuedToVoterId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponPools_RewardConfigId",
                table: "CouponPools",
                column: "RewardConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardConfigs_ConstituencyId",
                table: "RewardConfigs",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyCompletions_CouponId",
                table: "SurveyCompletions",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyCompletions_VoterId",
                table: "SurveyCompletions",
                column: "VoterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoterConsents_VoterId",
                table: "VoterConsents",
                column: "VoterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoterProfiles_VoterId",
                table: "VoterProfiles",
                column: "VoterId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SurveyCompletions");

            migrationBuilder.DropTable(
                name: "VoterConsents");

            migrationBuilder.DropTable(
                name: "VoterProfiles");

            migrationBuilder.DropTable(
                name: "CouponPools");

            migrationBuilder.DropTable(
                name: "RewardConfigs");
        }
    }
}
