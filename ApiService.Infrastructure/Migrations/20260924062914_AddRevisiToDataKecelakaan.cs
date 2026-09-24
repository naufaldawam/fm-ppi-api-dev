using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRevisiToDataKecelakaan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Revisi",
                table: "DataKecelakaans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Revisi",
                table: "DataKecelakaans");
        }
    }
}
