using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataDcu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyCheckUps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TanggalDcu = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TekananDarahSistolik = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TekananDarahDiastolik = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaturasiOksigen = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NadiDenyut = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SuhuTubuh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StatusKesehatan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Fit"),
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
                    table.PrimaryKey("PK_DailyCheckUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyCheckUps_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyCheckUpEvidences",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataDcuId = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                    table.PrimaryKey("PK_DailyCheckUpEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyCheckUpEvidences_DailyCheckUps_DataDcuId",
                        column: x => x.DataDcuId,
                        principalTable: "DailyCheckUps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUpEvidences_DataDcuId",
                table: "DailyCheckUpEvidences",
                column: "DataDcuId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUpEvidences_GeneratedName",
                table: "DailyCheckUpEvidences",
                column: "GeneratedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUpEvidences_IsDeleted",
                table: "DailyCheckUpEvidences",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUpEvidences_SortOrder",
                table: "DailyCheckUpEvidences",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUps_DriverId",
                table: "DailyCheckUps",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUps_DriverId_TanggalDcu",
                table: "DailyCheckUps",
                columns: new[] { "DriverId", "TanggalDcu" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUps_IsDeleted",
                table: "DailyCheckUps",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUps_StatusKesehatan",
                table: "DailyCheckUps",
                column: "StatusKesehatan");

            migrationBuilder.CreateIndex(
                name: "IX_DailyCheckUps_TanggalDcu",
                table: "DailyCheckUps",
                column: "TanggalDcu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyCheckUpEvidences");

            migrationBuilder.DropTable(
                name: "DailyCheckUps");
        }
    }
}
