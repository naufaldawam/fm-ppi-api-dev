using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TipeBahanbakarVendorKepemilikan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterBahanBakars",
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
                    table.PrimaryKey("PK_MasterBahanBakars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterKepemilikans",
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
                    table.PrimaryKey("PK_MasterKepemilikans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterTipes",
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
                    table.PrimaryKey("PK_MasterTipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterVendors",
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
                    table.PrimaryKey("PK_MasterVendors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterBahanBakars_IsDeleted",
                table: "MasterBahanBakars",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBahanBakars_Name",
                table: "MasterBahanBakars",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterKepemilikans_IsDeleted",
                table: "MasterKepemilikans",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MasterKepemilikans_Name",
                table: "MasterKepemilikans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterTipes_IsDeleted",
                table: "MasterTipes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MasterTipes_Name",
                table: "MasterTipes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterVendors_IsDeleted",
                table: "MasterVendors",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MasterVendors_Name",
                table: "MasterVendors",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterBahanBakars");

            migrationBuilder.DropTable(
                name: "MasterKepemilikans");

            migrationBuilder.DropTable(
                name: "MasterTipes");

            migrationBuilder.DropTable(
                name: "MasterVendors");
        }
    }
}
