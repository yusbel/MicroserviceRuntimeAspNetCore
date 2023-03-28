using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.PayRoll.Migrations
{
    public partial class removewelknownendpointandtlslikeenpoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CryptoEndpoint",
                table: "ExternalEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SingDataKey",
                table: "ExternalEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CryptoEndpoint",
                table: "ExternalEvents");

            migrationBuilder.DropColumn(
                name: "SingDataKey",
                table: "ExternalEvents");
        }
    }
}
