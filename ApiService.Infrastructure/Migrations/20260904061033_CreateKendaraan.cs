using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateKendaraan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kendaraans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NomorPolisi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BahanBakarId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Merek = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KepemilikanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JabatanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PekerjaId = table.Column<string>(type: "nvarchar(450)", nullable: true),
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
                    table.PrimaryKey("PK_Kendaraans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kendaraans_MasterBahanBakars_BahanBakarId",
                        column: x => x.BahanBakarId,
                        principalTable: "MasterBahanBakars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kendaraans_MasterJabatans_JabatanId",
                        column: x => x.JabatanId,
                        principalTable: "MasterJabatans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kendaraans_MasterKepemilikans_KepemilikanId",
                        column: x => x.KepemilikanId,
                        principalTable: "MasterKepemilikans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kendaraans_MasterTipes_TipeId",
                        column: x => x.TipeId,
                        principalTable: "MasterTipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kendaraans_MasterVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "MasterVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kendaraans_Pekerjas_PekerjaId",
                        column: x => x.PekerjaId,
                        principalTable: "Pekerjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_BahanBakarId",
                table: "Kendaraans",
                column: "BahanBakarId");

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_IsDeleted",
                table: "Kendaraans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_JabatanId",
                table: "Kendaraans",
                column: "JabatanId");

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_KepemilikanId",
                table: "Kendaraans",
                column: "KepemilikanId");

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_NomorPolisi",
                table: "Kendaraans",
                column: "NomorPolisi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_PekerjaId",
                table: "Kendaraans",
                column: "PekerjaId");

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_TipeId",
                table: "Kendaraans",
                column: "TipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Kendaraans_VendorId",
                table: "Kendaraans",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kendaraans");
        }
    }
}
