using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAssignment",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Livraisons deja affectees avant cette migration : considerees comme acceptees (2 = Accepted)
            migrationBuilder.Sql("UPDATE \"Orders\" SET \"DeliveryAssignment\" = 2 WHERE \"DeliveryPersonId\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAssignment",
                table: "Orders");
        }
    }
}
