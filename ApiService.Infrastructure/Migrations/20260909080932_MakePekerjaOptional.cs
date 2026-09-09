using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakePekerjaOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Pekerjas_AtasanId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_RfIds_Pekerjas_PekerjaId",
                table: "RfIds");

            migrationBuilder.AlterColumn<string>(
                name: "PekerjaId",
                table: "RfIds",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "AtasanId",
                table: "Drivers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Pekerjas_AtasanId",
                table: "Drivers",
                column: "AtasanId",
                principalTable: "Pekerjas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RfIds_Pekerjas_PekerjaId",
                table: "RfIds",
                column: "PekerjaId",
                principalTable: "Pekerjas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Pekerjas_AtasanId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_RfIds_Pekerjas_PekerjaId",
                table: "RfIds");

            migrationBuilder.AlterColumn<string>(
                name: "PekerjaId",
                table: "RfIds",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AtasanId",
                table: "Drivers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Pekerjas_AtasanId",
                table: "Drivers",
                column: "AtasanId",
                principalTable: "Pekerjas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RfIds_Pekerjas_PekerjaId",
                table: "RfIds",
                column: "PekerjaId",
                principalTable: "Pekerjas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
