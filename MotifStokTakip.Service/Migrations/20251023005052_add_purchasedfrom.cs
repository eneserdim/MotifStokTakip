using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotifStokTakip.Service.Migrations
{
    /// <inheritdoc />
    public partial class add_purchasedfrom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurchasedFrom",
                table: "Products",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchasedFrom",
                table: "Products");
        }
    }
}
