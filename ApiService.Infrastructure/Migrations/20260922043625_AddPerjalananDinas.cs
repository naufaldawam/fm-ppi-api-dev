using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerjalananDinas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerjalananDinas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PekerjaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BulanTahun = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalBiayaDinas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_PerjalananDinas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerjalananDinas_Pekerjas_PekerjaId",
                        column: x => x.PekerjaId,
                        principalTable: "Pekerjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerjalananDinas_Periodes_PeriodeId",
                        column: x => x.PeriodeId,
                        principalTable: "Periodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerjalananDinas_BulanTahun",
                table: "PerjalananDinas",
                column: "BulanTahun");

            migrationBuilder.CreateIndex(
                name: "IX_PerjalananDinas_IsDeleted",
                table: "PerjalananDinas",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PerjalananDinas_PekerjaId",
                table: "PerjalananDinas",
                column: "PekerjaId");

            migrationBuilder.CreateIndex(
                name: "IX_PerjalananDinas_PeriodeId",
                table: "PerjalananDinas",
                column: "PeriodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerjalananDinas");
        }
    }
}
