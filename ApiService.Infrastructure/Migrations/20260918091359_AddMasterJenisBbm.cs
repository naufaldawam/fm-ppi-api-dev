using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterJenisBbm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterJenisBbm",
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
                    table.PrimaryKey("PK_MasterJenisBbm", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterJenisBbm_IsDeleted",
                table: "MasterJenisBbm",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MasterJenisBbm_Name",
                table: "MasterJenisBbm",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterJenisBbm");
        }
    }
}
