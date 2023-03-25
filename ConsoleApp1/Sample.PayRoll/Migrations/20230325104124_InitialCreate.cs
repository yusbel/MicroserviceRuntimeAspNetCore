using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.PayRoll.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CertificateLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificateKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MsgQueueEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MsgQueueName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MsgDecryptScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scheme = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsSent = table.Column<bool>(type: "bit", nullable: false),
                    SendFailReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ServiceInstanceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WasAcknowledge = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InComingEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CertificateLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificateKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MsgQueueEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MsgQueueName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MsgDecryptScope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WellknownEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecryptEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcknowledgementEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scheme = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MessageKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    WasAcknowledge = table.Column<bool>(type: "bit", nullable: false),
                    WasProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ServiceInstanceId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InComingEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayRoll",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MonthlySalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MailPaperRecord = table.Column<bool>(type: "bit", nullable: false),
                    EmployeeIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayRoll", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalEvents");

            migrationBuilder.DropTable(
                name: "InComingEvents");

            migrationBuilder.DropTable(
                name: "PayRoll");

            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
