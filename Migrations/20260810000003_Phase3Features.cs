using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GPS on visits
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "DoorToDoorVisits",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "DoorToDoorVisits",
                type: "REAL",
                nullable: true);

            // Push tokens
            migrationBuilder.CreateTable(
                name: "UserPushTokens",
                columns: table => new
                {
                    Id             = table.Column<int>(type: "INTEGER", nullable: false)
                                         .Annotation("Sqlite:Autoincrement", true),
                    UserId         = table.Column<string>(type: "TEXT", nullable: false),
                    ExpoPushToken  = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId       = table.Column<string>(type: "TEXT", nullable: true),
                    Platform       = table.Column<string>(type: "TEXT", nullable: true),
                    RegisteredAt   = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt     = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_UserPushTokens", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_UserPushTokens_UserId",
                table: "UserPushTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPushTokens_ExpoPushToken",
                table: "UserPushTokens",
                column: "ExpoPushToken",
                unique: true);

            // Volunteer locations
            migrationBuilder.CreateTable(
                name: "VolunteerLocations",
                columns: table => new
                {
                    Id             = table.Column<int>(type: "INTEGER", nullable: false)
                                         .Annotation("Sqlite:Autoincrement", true),
                    UserId         = table.Column<string>(type: "TEXT", nullable: false),
                    UserName       = table.Column<string>(type: "TEXT", nullable: false),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: true),
                    Latitude       = table.Column<double>(type: "REAL", nullable: false),
                    Longitude      = table.Column<double>(type: "REAL", nullable: false),
                    AccuracyMeters = table.Column<double>(type: "REAL", nullable: true),
                    UpdatedAt      = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_VolunteerLocations", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerLocations_UserId",
                table: "VolunteerLocations",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Latitude",  table: "DoorToDoorVisits");
            migrationBuilder.DropColumn(name: "Longitude", table: "DoorToDoorVisits");
            migrationBuilder.DropTable(name: "UserPushTokens");
            migrationBuilder.DropTable(name: "VolunteerLocations");
        }
    }
}
