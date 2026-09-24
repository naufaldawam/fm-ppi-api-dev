using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MemberParkirV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemberParkir_RfIdCode",
                table: "MemberParkir");

            migrationBuilder.DropColumn(
                name: "RfIdCode",
                table: "MemberParkir");

            migrationBuilder.AlterColumn<decimal>(
                name: "JumlahBiaya",
                table: "MemberParkir",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "JumlahBiaya",
                table: "MemberParkir",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "RfIdCode",
                table: "MemberParkir",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MemberParkir_RfIdCode",
                table: "MemberParkir",
                column: "RfIdCode",
                unique: true);
        }
    }
}
