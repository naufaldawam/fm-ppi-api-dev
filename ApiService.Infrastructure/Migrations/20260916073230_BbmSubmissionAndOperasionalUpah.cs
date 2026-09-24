using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BbmSubmissionAndOperasionalUpah : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BbmSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AtasanPekerjaId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PeriodeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TanggalPenggunaan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KendaraanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JumlahPenggunaanBbm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NilaiOdometer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NilaiNota = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FotoOdometerUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FotoNotaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CatatanTambahan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_BbmSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BbmSubmissions_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BbmSubmissions_Kendaraans_KendaraanId",
                        column: x => x.KendaraanId,
                        principalTable: "Kendaraans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BbmSubmissions_Pekerjas_AtasanPekerjaId",
                        column: x => x.AtasanPekerjaId,
                        principalTable: "Pekerjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BbmSubmissions_Periodes_PeriodeId",
                        column: x => x.PeriodeId,
                        principalTable: "Periodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperasionalUpah",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PekerjaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PeriodeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalLembur = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalEMoneyMember = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DanaOps = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalParkir = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalSewaKendaraan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalUpahDriver = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_OperasionalUpah", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperasionalUpah_Pekerjas_PekerjaId",
                        column: x => x.PekerjaId,
                        principalTable: "Pekerjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperasionalUpah_Periodes_PeriodeId",
                        column: x => x.PeriodeId,
                        principalTable: "Periodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BbmSubmissions_AtasanPekerjaId",
                table: "BbmSubmissions",
                column: "AtasanPekerjaId");

            migrationBuilder.CreateIndex(
                name: "IX_BbmSubmissions_DriverId",
                table: "BbmSubmissions",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_BbmSubmissions_IsDeleted",
                table: "BbmSubmissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BbmSubmissions_KendaraanId",
                table: "BbmSubmissions",
                column: "KendaraanId");

            migrationBuilder.CreateIndex(
                name: "IX_BbmSubmissions_PeriodeId",
                table: "BbmSubmissions",
                column: "PeriodeId");

            migrationBuilder.CreateIndex(
                name: "IX_BbmSubmissions_Status",
                table: "BbmSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OperasionalUpah_IsDeleted",
                table: "OperasionalUpah",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OperasionalUpah_PekerjaId_PeriodeId",
                table: "OperasionalUpah",
                columns: new[] { "PekerjaId", "PeriodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperasionalUpah_PeriodeId",
                table: "OperasionalUpah",
                column: "PeriodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BbmSubmissions");

            migrationBuilder.DropTable(
                name: "OperasionalUpah");
        }
    }
}
