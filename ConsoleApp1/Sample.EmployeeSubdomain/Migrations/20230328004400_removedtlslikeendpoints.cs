using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.EmployeeSubdomain.Migrations
{
    public partial class removedtlslikeendpoints : Migration
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
