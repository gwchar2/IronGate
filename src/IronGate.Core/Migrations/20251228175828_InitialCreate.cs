using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronGate.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "db_config_profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HashAlgorithm = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PepperEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RateLimitEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RateLimitWindowSeconds = table.Column<int>(type: "int", nullable: true),
                    MaxAttemptsPerUser = table.Column<int>(type: "int", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutThreshold = table.Column<int>(type: "int", nullable: true),
                    LockoutDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    CaptchaEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CaptchaAfterFailedAttempts = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_db_config_profile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlainPassword = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordStrengthCategory = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailedAttemptsInWindow = table.Column<int>(type: "int", nullable: false),
                    LockoutUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginSuccessAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotpEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TotpSecret = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TotpRegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CaptchaRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_hashes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashAlgorithm = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PepperEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_hashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_hashes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_hashes_UserId_HashAlgorithm_PepperEnabled",
                table: "user_hashes",
                columns: new[] { "UserId", "HashAlgorithm", "PepperEnabled" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "db_config_profile");

            migrationBuilder.DropTable(
                name: "user_hashes");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
