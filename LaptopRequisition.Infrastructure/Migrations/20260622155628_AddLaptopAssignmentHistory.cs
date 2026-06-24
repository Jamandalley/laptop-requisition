using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopRequisition.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLaptopAssignmentHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LaptopAssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    LaptopId = table.Column<Guid>(type: "char(36)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaptopAssignmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaptopAssignmentHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LaptopAssignmentHistories_Laptops_LaptopId",
                        column: x => x.LaptopId,
                        principalTable: "Laptops",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 15, 56, 24, 481, DateTimeKind.Utc).AddTicks(6308), new DateTime(2026, 6, 22, 15, 56, 24, 481, DateTimeKind.Utc).AddTicks(6312) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 15, 56, 24, 481, DateTimeKind.Utc).AddTicks(8191), new DateTime(2026, 6, 22, 15, 56, 24, 481, DateTimeKind.Utc).AddTicks(8193) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 15, 56, 24, 481, DateTimeKind.Utc).AddTicks(8199), new DateTime(2026, 6, 22, 15, 56, 24, 481, DateTimeKind.Utc).AddTicks(8200) });

            migrationBuilder.CreateIndex(
                name: "IX_LaptopAssignmentHistories_EmployeeId",
                table: "LaptopAssignmentHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LaptopAssignmentHistories_LaptopId",
                table: "LaptopAssignmentHistories",
                column: "LaptopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaptopAssignmentHistories");

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 19, 16, 23, 18, 738, DateTimeKind.Utc).AddTicks(4995), new DateTime(2026, 6, 19, 16, 23, 18, 738, DateTimeKind.Utc).AddTicks(4999) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 19, 16, 23, 18, 738, DateTimeKind.Utc).AddTicks(7837), new DateTime(2026, 6, 19, 16, 23, 18, 738, DateTimeKind.Utc).AddTicks(7838) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 19, 16, 23, 18, 738, DateTimeKind.Utc).AddTicks(7844), new DateTime(2026, 6, 19, 16, 23, 18, 738, DateTimeKind.Utc).AddTicks(7845) });
        }
    }
}
