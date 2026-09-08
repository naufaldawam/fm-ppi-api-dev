using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVendorFromKendaraanV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kendaraans_MasterVendors_VendorId",
                table: "Kendaraans");

            migrationBuilder.DropIndex(
                name: "IX_Kendaraans_VendorId",
                table: "Kendaraans");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "Kendaraans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VendorId",
                table: "Kendaraans",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_VendorId",
                table: "Kendaraans",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kendaraans_MasterVendors_VendorId",
                table: "Kendaraans",
                column: "VendorId",
                principalTable: "MasterVendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
