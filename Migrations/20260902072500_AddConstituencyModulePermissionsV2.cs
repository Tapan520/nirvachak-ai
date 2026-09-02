using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddConstituencyModulePermissionsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConstituencyModulePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConstituencyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModuleKey = table.Column<string>(type: "TEXT", nullable: false),
                    SubModuleKey = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstituencyModulePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstituencyModulePermissions_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConstituencyModulePermissions_ConstituencyId_ModuleKey_SubModuleKey",
                table: "ConstituencyModulePermissions",
                columns: new[] { "ConstituencyId", "ModuleKey", "SubModuleKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstituencyModulePermissions");
        }
    }
}
