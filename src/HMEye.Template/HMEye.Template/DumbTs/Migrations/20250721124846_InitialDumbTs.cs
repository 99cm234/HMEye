using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HMEye.DumbTs.Migrations
{
    /// <inheritdoc />
    public partial class InitialDumbTs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BooleanDataPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeriesName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Timestamp = table.Column<long>(type: "timestamp", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DataType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BooleanDataPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NumericDataPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<double>(type: "decimal(28,8)", nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    OriginalType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SeriesName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Timestamp = table.Column<long>(type: "timestamp", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DataType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumericDataPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TextDataPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SeriesName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Timestamp = table.Column<long>(type: "timestamp", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DataType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextDataPoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BooleanDataPoints_SeriesName_Timestamp",
                table: "BooleanDataPoints",
                columns: new[] { "SeriesName", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NumericDataPoints_SeriesName_Timestamp",
                table: "NumericDataPoints",
                columns: new[] { "SeriesName", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_TextDataPoints_SeriesName_Timestamp",
                table: "TextDataPoints",
                columns: new[] { "SeriesName", "Timestamp" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BooleanDataPoints");

            migrationBuilder.DropTable(
                name: "NumericDataPoints");

            migrationBuilder.DropTable(
                name: "TextDataPoints");
        }
    }
}
