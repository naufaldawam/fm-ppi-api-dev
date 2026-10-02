using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInspeksiKendaraan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InspeksiKendaraans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KendaraanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TanggalInspeksi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KelayakanJalan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspeksiKendaraans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspeksiKendaraans_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InspeksiKendaraans_Kendaraans_KendaraanId",
                        column: x => x.KendaraanId,
                        principalTable: "Kendaraans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InspeksiKendaraanDetails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InspeksiKendaraanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Kategori = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NamaPemeriksaan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsOk = table.Column<bool>(type: "bit", nullable: false),
                    Keterangan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspeksiKendaraanDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspeksiKendaraanDetails_InspeksiKendaraans_InspeksiKendaraanId",
                        column: x => x.InspeksiKendaraanId,
                        principalTable: "InspeksiKendaraans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiKendaraanDetails_InspeksiKendaraanId",
                table: "InspeksiKendaraanDetails",
                column: "InspeksiKendaraanId");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiKendaraanDetails_InspeksiKendaraanId_SortOrder",
                table: "InspeksiKendaraanDetails",
                columns: new[] { "InspeksiKendaraanId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiKendaraanDetails_IsDeleted",
                table: "InspeksiKendaraanDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiKendaraans_DriverId",
                table: "InspeksiKendaraans",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiKendaraans_IsDeleted",
                table: "InspeksiKendaraans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiKendaraans_KelayakanJalan",
                table: "InspeksiKendaraans",
                column: "KelayakanJalan");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiKendaraans_KendaraanId",
                table: "InspeksiKendaraans",
                column: "KendaraanId");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiKendaraans_TanggalInspeksi",
                table: "InspeksiKendaraans",
                column: "TanggalInspeksi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InspeksiKendaraanDetails");

            migrationBuilder.DropTable(
                name: "InspeksiKendaraans");
        }
    }
}
