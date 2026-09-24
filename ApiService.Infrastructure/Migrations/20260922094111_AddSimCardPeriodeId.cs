using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimCardPeriodeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PeriodeId",
                table: "SimCards",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BiayaKesehatans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PekerjaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BulanTahun = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalBiaya = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_BiayaKesehatans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BiayaKesehatans_Pekerjas_PekerjaId",
                        column: x => x.PekerjaId,
                        principalTable: "Pekerjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BiayaKesehatans_Periodes_PeriodeId",
                        column: x => x.PeriodeId,
                        principalTable: "Periodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimCards_PeriodeId",
                table: "SimCards",
                column: "PeriodeId");

            migrationBuilder.CreateIndex(
                name: "IX_BiayaKesehatans_BulanTahun",
                table: "BiayaKesehatans",
                column: "BulanTahun");

            migrationBuilder.CreateIndex(
                name: "IX_BiayaKesehatans_IsDeleted",
                table: "BiayaKesehatans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BiayaKesehatans_PekerjaId",
                table: "BiayaKesehatans",
                column: "PekerjaId");

            migrationBuilder.CreateIndex(
                name: "IX_BiayaKesehatans_PeriodeId",
                table: "BiayaKesehatans",
                column: "PeriodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SimCards_Periodes_PeriodeId",
                table: "SimCards",
                column: "PeriodeId",
                principalTable: "Periodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimCards_Periodes_PeriodeId",
                table: "SimCards");

            migrationBuilder.DropTable(
                name: "BiayaKesehatans");

            migrationBuilder.DropIndex(
                name: "IX_SimCards_PeriodeId",
                table: "SimCards");

            migrationBuilder.DropColumn(
                name: "PeriodeId",
                table: "SimCards");
        }
    }
}
