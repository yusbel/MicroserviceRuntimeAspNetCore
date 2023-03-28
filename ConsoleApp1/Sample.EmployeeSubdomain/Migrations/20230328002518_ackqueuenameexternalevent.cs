using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.EmployeeSubdomain.Migrations
{
    public partial class ackqueuenameexternalevent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AckQueueName",
                table: "ExternalEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AckQueueName",
                table: "ExternalEvents");
        }
    }
}
