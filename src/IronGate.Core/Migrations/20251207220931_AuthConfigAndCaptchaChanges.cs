using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronGate.Core.Migrations
{
    /// <inheritdoc />
    public partial class AuthConfigAndCaptchaChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PepperEnabled",
                table: "user_hashes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PepperEnabled",
                table: "user_hashes");
        }
    }
}
