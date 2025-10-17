using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MotifStokTakip.Service.Migrations
{
    /// <inheritdoc />
    public partial class userupgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 10, 0, 17, 15, 513, DateTimeKind.Utc).AddTicks(4570));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "FullName", "IsActive", "IsDeleted", "PasswordHash", "Role", "UpdatedAt", "UserName" },
                values: new object[,]
                {
                    { 2, new DateTime(2025, 10, 10, 0, 17, 15, 513, DateTimeKind.Utc).AddTicks(4571), "Muhasebe Kullanıcısı", true, false, "3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121", 2, null, "muhasebe" },
                    { 3, new DateTime(2025, 10, 10, 0, 17, 15, 513, DateTimeKind.Utc).AddTicks(4573), "Usta Kullanıcısı", true, false, "3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121", 3, null, "usta" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 10, 0, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}
