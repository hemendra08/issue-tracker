using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorIdentitySchemaExact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "Users",
                newName: "FullName");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateTable(
                name: "UserCredentials",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CredentialData = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCredentials", x => new { x.UserId, x.Type });
                    table.ForeignKey(
                        name: "FK_UserCredentials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserIdentifiers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserIdentifiers", x => new { x.UserId, x.Type });
                    table.ForeignKey(
                        name: "FK_UserIdentifiers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentifiers_Type_Value",
                table: "UserIdentifiers",
                columns: new[] { "Type", "Value" },
                unique: true);

            // Backfill existing user data into identifiers and credentials before dropping legacy columns
            migrationBuilder.Sql("INSERT INTO \"UserIdentifiers\" (\"UserId\", \"Type\", \"Value\") SELECT \"Id\", 'Email', \"Email\" FROM \"Users\" ON CONFLICT DO NOTHING;");
            migrationBuilder.Sql("INSERT INTO \"UserCredentials\" (\"UserId\", \"Type\", \"CredentialData\") SELECT \"Id\", 'Password', \"PasswordHash\" FROM \"Users\" ON CONFLICT DO NOTHING;");

            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"Users\" u SET \"Email\" = COALESCE((SELECT \"Value\" FROM \"UserIdentifiers\" WHERE \"UserId\" = u.\"Id\" AND \"Type\" = 'Email' LIMIT 1), ''), \"NormalizedEmail\" = UPPER(COALESCE((SELECT \"Value\" FROM \"UserIdentifiers\" WHERE \"UserId\" = u.\"Id\" AND \"Type\" = 'Email' LIMIT 1), ''));");
            migrationBuilder.Sql("UPDATE \"Users\" u SET \"PasswordHash\" = COALESCE((SELECT \"CredentialData\" FROM \"UserCredentials\" WHERE \"UserId\" = u.\"Id\" AND \"Type\" = 'Password' LIMIT 1), '');");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Users",
                newName: "DisplayName");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropTable(
                name: "UserCredentials");

            migrationBuilder.DropTable(
                name: "UserIdentifiers");
        }
    }
}
