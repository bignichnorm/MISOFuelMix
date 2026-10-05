using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MISOQueryingApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateMisoFuelMixDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuelMixSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IntervalEst = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TotalMegaWatts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelMixSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuelMixElements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SnapshotId = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MegaWatts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelMixElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelMixElements_FuelMixSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "FuelMixSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuelMixElements_Category",
                table: "FuelMixElements",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FuelMixElements_SnapshotId",
                table: "FuelMixElements",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelMixSnapshots_IntervalEst",
                table: "FuelMixSnapshots",
                column: "IntervalEst",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelMixElements");

            migrationBuilder.DropTable(
                name: "FuelMixSnapshots");
        }
    }
}
