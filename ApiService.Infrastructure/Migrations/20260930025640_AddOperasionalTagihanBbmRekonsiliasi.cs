using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperasionalTagihanBbmRekonsiliasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperasionalTagihanBbmRekonsiliasis",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PeriodeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BbmSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TanggalRitel = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NomorReferensiRitel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoPekerjaRitel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NomorPolisiRitel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NamaRitel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    JumlahBbmRitel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NilaiRitel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    KendaraanId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    JumlahBbmDriver = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NilaiNotaDriver = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TanggalDriver = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusMatching = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MismatchReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: true),
                    MatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_OperasionalTagihanBbmRekonsiliasis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperasionalTagihanBbmRekonsiliasis_BbmSubmissions_BbmSubmissionId",
                        column: x => x.BbmSubmissionId,
                        principalTable: "BbmSubmissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperasionalTagihanBbmRekonsiliasis_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperasionalTagihanBbmRekonsiliasis_Kendaraans_KendaraanId",
                        column: x => x.KendaraanId,
                        principalTable: "Kendaraans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperasionalTagihanBbmRekonsiliasis_Periodes_PeriodeId",
                        column: x => x.PeriodeId,
                        principalTable: "Periodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperasionalTagihanBbmRekonsiliasis_BbmSubmissionId",
                table: "OperasionalTagihanBbmRekonsiliasis",
                column: "BbmSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasionalTagihanBbmRekonsiliasis_DriverId",
                table: "OperasionalTagihanBbmRekonsiliasis",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasionalTagihanBbmRekonsiliasis_KendaraanId",
                table: "OperasionalTagihanBbmRekonsiliasis",
                column: "KendaraanId");

            migrationBuilder.CreateIndex(
                name: "IX_OperasionalTagihanBbmRekonsiliasis_PeriodeId",
                table: "OperasionalTagihanBbmRekonsiliasis",
                column: "PeriodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperasionalTagihanBbmRekonsiliasis");
        }
    }
}
