using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberParkir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberParkir",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PekerjaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JabatanId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RfIdCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TanggalPenagihan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JumlahBiaya = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_MemberParkir", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberParkir_MasterJabatans_JabatanId",
                        column: x => x.JabatanId,
                        principalTable: "MasterJabatans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberParkir_Pekerjas_PekerjaId",
                        column: x => x.PekerjaId,
                        principalTable: "Pekerjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberParkir_IsDeleted",
                table: "MemberParkir",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MemberParkir_JabatanId",
                table: "MemberParkir",
                column: "JabatanId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberParkir_PekerjaId",
                table: "MemberParkir",
                column: "PekerjaId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberParkir_RfIdCode",
                table: "MemberParkir",
                column: "RfIdCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberParkir");
        }
    }
}
