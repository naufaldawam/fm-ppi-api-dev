using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreatePekerjaJabatan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterJabatans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_MasterJabatans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pekerjas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NoPekerja = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NopekHome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NopekHost = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NamaPekerja = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    JabatanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RfIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Pekerjas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pekerjas_MasterJabatans_JabatanId",
                        column: x => x.JabatanId,
                        principalTable: "MasterJabatans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterJabatans_IsDeleted",
                table: "MasterJabatans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MasterJabatans_Name",
                table: "MasterJabatans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pekerjas_IsDeleted",
                table: "Pekerjas",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Pekerjas_JabatanId",
                table: "Pekerjas",
                column: "JabatanId");

            migrationBuilder.CreateIndex(
                name: "IX_Pekerjas_NamaPekerja",
                table: "Pekerjas",
                column: "NamaPekerja");

            migrationBuilder.CreateIndex(
                name: "IX_Pekerjas_NoPekerja",
                table: "Pekerjas",
                column: "NoPekerja",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pekerjas");

            migrationBuilder.DropTable(
                name: "MasterJabatans");
        }
    }
}
