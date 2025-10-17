using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotifStokTakip.Service.Migrations
{
    /// <inheritdoc />
    public partial class RetailPos_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetailSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    WalkInCustomerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailSales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RetailPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetailSaleId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailPayments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RetailPayments_RetailSales_RetailSaleId",
                        column: x => x.RetailSaleId,
                        principalTable: "RetailSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RetailSaleLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetailSaleId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    BuyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SellPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailSaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailSaleLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetailSaleLines_RetailSales_RetailSaleId",
                        column: x => x.RetailSaleId,
                        principalTable: "RetailSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 13, 0, 9, 33, 143, DateTimeKind.Utc).AddTicks(292));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 13, 0, 9, 33, 143, DateTimeKind.Utc).AddTicks(295));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 13, 0, 9, 33, 143, DateTimeKind.Utc).AddTicks(297));

            migrationBuilder.CreateIndex(
                name: "IX_RetailPayments_CustomerId",
                table: "RetailPayments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailPayments_RetailSaleId",
                table: "RetailPayments",
                column: "RetailSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailSaleLines_ProductId",
                table: "RetailSaleLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailSaleLines_RetailSaleId",
                table: "RetailSaleLines",
                column: "RetailSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailSales_CustomerId",
                table: "RetailSales",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetailPayments");

            migrationBuilder.DropTable(
                name: "RetailSaleLines");

            migrationBuilder.DropTable(
                name: "RetailSales");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 12, 23, 30, 33, 22, DateTimeKind.Utc).AddTicks(6778));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 12, 23, 30, 33, 22, DateTimeKind.Utc).AddTicks(6780));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 12, 23, 30, 33, 22, DateTimeKind.Utc).AddTicks(6782));
        }
    }
}
