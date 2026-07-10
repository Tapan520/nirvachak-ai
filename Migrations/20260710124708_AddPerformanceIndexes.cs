using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nirvachak_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Voters_ConstituencyId_IsDeleted",
                table: "Voters",
                columns: new[] { "ConstituencyId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Voters_ConstituencyId_Sentiment",
                table: "Voters",
                columns: new[] { "ConstituencyId", "Sentiment" });

            migrationBuilder.CreateIndex(
                name: "IX_Voters_LastContactedAt",
                table: "Voters",
                column: "LastContactedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Voters_Sentiment",
                table: "Voters",
                column: "Sentiment");

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_IsActive",
                table: "Volunteers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Grievances_ConstituencyId_Status",
                table: "Grievances",
                columns: new[] { "ConstituencyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Grievances_Status",
                table: "Grievances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Voters_ConstituencyId_IsDeleted",
                table: "Voters");

            migrationBuilder.DropIndex(
                name: "IX_Voters_ConstituencyId_Sentiment",
                table: "Voters");

            migrationBuilder.DropIndex(
                name: "IX_Voters_LastContactedAt",
                table: "Voters");

            migrationBuilder.DropIndex(
                name: "IX_Voters_Sentiment",
                table: "Voters");

            migrationBuilder.DropIndex(
                name: "IX_Volunteers_IsActive",
                table: "Volunteers");

            migrationBuilder.DropIndex(
                name: "IX_Grievances_ConstituencyId_Status",
                table: "Grievances");

            migrationBuilder.DropIndex(
                name: "IX_Grievances_Status",
                table: "Grievances");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses");
        }
    }
}
