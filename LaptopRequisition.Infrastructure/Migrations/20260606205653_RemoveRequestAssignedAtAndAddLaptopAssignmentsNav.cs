using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopRequisition.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRequestAssignedAtAndAddLaptopAssignmentsNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Requests");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "LaptopAssignments",
                type: "char(36)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 6, 20, 56, 51, 798, DateTimeKind.Utc).AddTicks(6514), new DateTime(2026, 6, 6, 20, 56, 51, 798, DateTimeKind.Utc).AddTicks(6517) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 6, 20, 56, 51, 798, DateTimeKind.Utc).AddTicks(8071), new DateTime(2026, 6, 6, 20, 56, 51, 798, DateTimeKind.Utc).AddTicks(8072) });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 6, 20, 56, 51, 798, DateTimeKind.Utc).AddTicks(8078), new DateTime(2026, 6, 6, 20, 56, 51, 798, DateTimeKind.Utc).AddTicks(8078) });

            migrationBuilder.CreateIndex(
                name: "IX_LaptopAssignments_RequestId",
                table: "LaptopAssignments",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_LaptopAssignments_Requests_RequestId",
                table: "LaptopAssignments",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaptopAssignments_Requests_RequestId",
                table: "LaptopAssignments");

            migrationBuilder.DropIndex(
                name: "IX_LaptopAssignments_RequestId",
                table: "LaptopAssignments");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "LaptopAssignments");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Requests",
                type: "datetime(6)",
                nullable: true);

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
        }
    }
}
