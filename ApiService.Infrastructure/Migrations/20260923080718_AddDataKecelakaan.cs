using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataKecelakaan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataKecelakaans_Nomor",
                table: "DataKecelakaans");

            migrationBuilder.AlterColumn<string>(
                name: "WaktuKejadian",
                table: "DataKecelakaans",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_Nomor",
                table: "DataKecelakaans",
                column: "Nomor",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataKecelakaans_Nomor",
                table: "DataKecelakaans");

            migrationBuilder.AlterColumn<long>(
                name: "WaktuKejadian",
                table: "DataKecelakaans",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_Nomor",
                table: "DataKecelakaans",
                column: "Nomor");
        }
    }
}
