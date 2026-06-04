using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopRequisition.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LaptopAssignmentsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Laptops_Employees_AssignedToEmployeeId",
                table: "Laptops");

            migrationBuilder.DropIndex(
                name: "IX_Laptops_AssignedToEmployeeId",
                table: "Laptops");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Laptops");

            migrationBuilder.DropColumn(
                name: "AssignedToEmployeeId",
                table: "Laptops");

            migrationBuilder.CreateTable(
                name: "LaptopAssignments",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    LaptopId = table.Column<Guid>(type: "char(36)", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EmployeeId1 = table.Column<Guid>(type: "char(36)", nullable: true),
                    LaptopId1 = table.Column<Guid>(type: "char(36)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaptopAssignments", x => new { x.EmployeeId, x.LaptopId });
                    table.ForeignKey(
                        name: "FK_LaptopAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LaptopAssignments_Employees_EmployeeId1",
                        column: x => x.EmployeeId1,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LaptopAssignments_Laptops_LaptopId",
                        column: x => x.LaptopId,
                        principalTable: "Laptops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LaptopAssignments_Laptops_LaptopId1",
                        column: x => x.LaptopId1,
                        principalTable: "Laptops",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 4, 18, 42, 27, 743, DateTimeKind.Utc).AddTicks(6093), new DateTime(2026, 6, 4, 18, 42, 27, 743, DateTimeKind.Utc).AddTicks(6097) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 4, 18, 42, 27, 743, DateTimeKind.Utc).AddTicks(7643), new DateTime(2026, 6, 4, 18, 42, 27, 743, DateTimeKind.Utc).AddTicks(7644) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 4, 18, 42, 27, 743, DateTimeKind.Utc).AddTicks(7652), new DateTime(2026, 6, 4, 18, 42, 27, 743, DateTimeKind.Utc).AddTicks(7653) });

            migrationBuilder.CreateIndex(
                name: "IX_LaptopAssignments_EmployeeId1",
                table: "LaptopAssignments",
                column: "EmployeeId1");

            migrationBuilder.CreateIndex(
                name: "IX_LaptopAssignments_LaptopId",
                table: "LaptopAssignments",
                column: "LaptopId");

            migrationBuilder.CreateIndex(
                name: "IX_LaptopAssignments_LaptopId1",
                table: "LaptopAssignments",
                column: "LaptopId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaptopAssignments");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Laptops",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToEmployeeId",
                table: "Laptops",
                type: "char(36)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(2116), new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(2119) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3476), new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3477) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3484), new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3485) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3490), "Engineer specializing in backend development", "Backend Engineer", new DateTime(2026, 6, 2, 11, 45, 51, 879, DateTimeKind.Utc).AddTicks(3490) });

            migrationBuilder.CreateIndex(
                name: "IX_Laptops_AssignedToEmployeeId",
                table: "Laptops",
                column: "AssignedToEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Laptops_Employees_AssignedToEmployeeId",
                table: "Laptops",
                column: "AssignedToEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
