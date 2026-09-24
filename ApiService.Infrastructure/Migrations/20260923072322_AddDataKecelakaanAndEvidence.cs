using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataKecelakaanAndEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataKecelakaans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Judul = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    KategoriId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TanggalKejadian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaktuKejadian = table.Column<long>(type: "bigint", nullable: false),
                    Dampak = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PejabatId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Alamat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DetailKejadian = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PenyebabKejadian = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BagaimanaTerjadinya = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AkarPermasalahan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TindakanSegara = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TindakanPerbaikan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    Nomor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_DataKecelakaans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataKecelakaans_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataKecelakaans_MasterKategoriKecelakaan_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "MasterKategoriKecelakaan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataKecelakaans_Pekerjas_PejabatId",
                        column: x => x.PejabatId,
                        principalTable: "Pekerjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceKecelakaans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataKecelakaanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    GeneratedName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_EvidenceKecelakaans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceKecelakaans_DataKecelakaans_DataKecelakaanId",
                        column: x => x.DataKecelakaanId,
                        principalTable: "DataKecelakaans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_DriverId",
                table: "DataKecelakaans",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_IsDeleted",
                table: "DataKecelakaans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_KategoriId",
                table: "DataKecelakaans",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_Nomor",
                table: "DataKecelakaans",
                column: "Nomor");

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_PejabatId",
                table: "DataKecelakaans",
                column: "PejabatId");

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_Status",
                table: "DataKecelakaans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataKecelakaans_TanggalKejadian",
                table: "DataKecelakaans",
                column: "TanggalKejadian");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceKecelakaans_DataKecelakaanId",
                table: "EvidenceKecelakaans",
                column: "DataKecelakaanId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceKecelakaans_GeneratedName",
                table: "EvidenceKecelakaans",
                column: "GeneratedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceKecelakaans_IsDeleted",
                table: "EvidenceKecelakaans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceKecelakaans_SortOrder",
                table: "EvidenceKecelakaans",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenceKecelakaans");

            migrationBuilder.DropTable(
                name: "DataKecelakaans");
        }
    }
}
