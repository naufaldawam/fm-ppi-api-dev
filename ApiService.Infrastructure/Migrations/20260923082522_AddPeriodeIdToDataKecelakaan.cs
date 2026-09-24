using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodeIdToDataKecelakaan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PeriodeId",
                table: "DataKecelakaans",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_PeriodeId",
                table: "DataKecelakaans",
                column: "PeriodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_DataKecelakaans_Periodes_PeriodeId",
                table: "DataKecelakaans",
                column: "PeriodeId",
                principalTable: "Periodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataKecelakaans_Periodes_PeriodeId",
                table: "DataKecelakaans");

            migrationBuilder.DropIndex(
                name: "IX_DataKecelakaans_PeriodeId",
                table: "DataKecelakaans");

            migrationBuilder.DropColumn(
                name: "PeriodeId",
                table: "DataKecelakaans");
        }
    }
}
