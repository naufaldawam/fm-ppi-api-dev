using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodeIdToMemberParkir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PeriodeId",
                table: "MemberParkir",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE MemberParkir SET PeriodeId = (SELECT TOP 1 Id FROM Periodes ORDER BY Id)");

            migrationBuilder.CreateIndex(
                name: "IX_MemberParkir_PeriodeId",
                table: "MemberParkir",
                column: "PeriodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberParkir_Periodes_PeriodeId",
                table: "MemberParkir",
                column: "PeriodeId",
                principalTable: "Periodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberParkir_Periodes_PeriodeId",
                table: "MemberParkir");

            migrationBuilder.DropIndex(
                name: "IX_MemberParkir_PeriodeId",
                table: "MemberParkir");

            migrationBuilder.DropColumn(
                name: "PeriodeId",
                table: "MemberParkir");
        }
    }
}
