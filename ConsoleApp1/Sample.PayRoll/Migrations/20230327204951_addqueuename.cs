using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.PayRoll.Migrations
{
    public partial class addqueuename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AckQueueName",
                table: "InComingEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CryptoEndpoint",
                table: "InComingEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignDataKeyId",
                table: "InComingEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AckQueueName",
                table: "InComingEvents");

            migrationBuilder.DropColumn(
                name: "CryptoEndpoint",
                table: "InComingEvents");

            migrationBuilder.DropColumn(
                name: "SignDataKeyId",
                table: "InComingEvents");
        }
    }
}
