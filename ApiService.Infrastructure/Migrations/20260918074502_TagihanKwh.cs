using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TagihanKwh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TagihanKwhs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PeriodeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TanggalPenagihan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JumlahBiaya = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JumlahKwh = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Kategori = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "p8"),
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
                    table.PrimaryKey("PK_TagihanKwhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagihanKwhs_Periodes_PeriodeId",
                        column: x => x.PeriodeId,
                        principalTable: "Periodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TagihanKwhs_IsDeleted",
                table: "TagihanKwhs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TagihanKwhs_PeriodeId",
                table: "TagihanKwhs",
                column: "PeriodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagihanKwhs");
        }
    }
}
