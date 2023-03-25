using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.PayRoll.Migrations
{
    public partial class UpdateMonthlyColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PayRoll",
                table: "PayRoll");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlySalary",
                table: "PayRoll",
                type: "decimal",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayRoll",
                table: "PayRoll",
                column: "Id")
                .Annotation("SqlServer:Clustered", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PayRoll",
                table: "PayRoll");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlySalary",
                table: "PayRoll",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayRoll",
                table: "PayRoll",
                column: "Id");
        }
    }
}
