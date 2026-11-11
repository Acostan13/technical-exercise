using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StargateAPI.Business.Migrations
{
    /// <inheritdoc />
    public partial class EnforceDutyAndNameRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AstronautDuty_PersonId",
                table: "AstronautDuty");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Person",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_AstronautDuty_PersonId_Current",
                table: "AstronautDuty",
                column: "PersonId",
                unique: true,
                filter: "\"DutyEndDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AstronautDuty_PersonId_DutyStartDate",
                table: "AstronautDuty",
                columns: new[] { "PersonId", "DutyStartDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AstronautDuty_PersonId_Current",
                table: "AstronautDuty");

            migrationBuilder.DropIndex(
                name: "IX_AstronautDuty_PersonId_DutyStartDate",
                table: "AstronautDuty");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Person",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");

            migrationBuilder.CreateIndex(
                name: "IX_AstronautDuty_PersonId",
                table: "AstronautDuty",
                column: "PersonId");
        }
    }
}
