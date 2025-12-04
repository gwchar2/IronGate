using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IronGate.Core.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "defense_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsePepper = table.Column<bool>(type: "bit", nullable: false),
                    UseRateLimiting = table.Column<bool>(type: "bit", nullable: false),
                    MaxAttemptsPerMinute = table.Column<int>(type: "int", nullable: true),
                    UseCaptcha = table.Column<bool>(type: "bit", nullable: false),
                    UseTotp = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defense_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hash_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Algorithm = table.Column<int>(type: "int", nullable: false),
                    Iterations = table.Column<int>(type: "int", nullable: true),
                    MemoryKb = table.Column<int>(type: "int", nullable: true),
                    Parallelism = table.Column<int>(type: "int", nullable: true),
                    SaltMode = table.Column<int>(type: "int", nullable: false),
                    PepperMode = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hash_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sha256Salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Argon2Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Argon2Salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BcryptHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BcryptSalt = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "experiment_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefenseProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMs = table.Column<double>(type: "float", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalAttempts = table.Column<long>(type: "bigint", nullable: false),
                    AttemptsPerSecond = table.Column<double>(type: "float", nullable: false),
                    TimeToFirstSuccessMs = table.Column<double>(type: "float", nullable: true),
                    AverageLatencyMs = table.Column<double>(type: "float", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiment_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experiment_runs_defense_profiles_DefenseProfileId",
                        column: x => x.DefenseProfileId,
                        principalTable: "defense_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_experiment_runs_hash_profiles_HashProfileId",
                        column: x => x.HashProfileId,
                        principalTable: "hash_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_experiment_runs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_experiment_runs_DefenseProfileId",
                table: "experiment_runs",
                column: "DefenseProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_experiment_runs_HashProfileId_DefenseProfileId",
                table: "experiment_runs",
                columns: new[] { "HashProfileId", "DefenseProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_experiment_runs_StartedAtUtc",
                table: "experiment_runs",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_experiment_runs_UserId",
                table: "experiment_runs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_UserName",
                table: "users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experiment_runs");

            migrationBuilder.DropTable(
                name: "defense_profiles");

            migrationBuilder.DropTable(
                name: "hash_profiles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
