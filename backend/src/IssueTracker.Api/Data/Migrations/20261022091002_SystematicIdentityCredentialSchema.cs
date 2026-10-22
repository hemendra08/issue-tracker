using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IssueTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SystematicIdentityCredentialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Identities");

            migrationBuilder.CreateTable(
                name: "CredentialTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredentialTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialTypeId = table.Column<int>(type: "integer", nullable: false),
                    CredentialData = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Credentials_CredentialTypes_CredentialTypeId",
                        column: x => x.CredentialTypeId,
                        principalTable: "CredentialTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credentials_Identities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "Identities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CredentialIdentifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredentialIdentifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CredentialIdentifiers_Credentials_CredentialId",
                        column: x => x.CredentialId,
                        principalTable: "Credentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CredentialIdentifiers_Identities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "Identities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CredentialTypes",
                columns: new[] { "Id", "Code", "CreatedAtUtc", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Password", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standard salted password authentication using PBKDF2", "Password Authentication" },
                    { 2, "OAuth_Google", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Google OpenID Connect identity provider", "Google OAuth 2.0" },
                    { 3, "OAuth_GitHub", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "GitHub Developer OAuth identity provider", "GitHub OAuth" },
                    { 4, "ApiKey", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Programmatic service-to-service API key authentication", "API Key" }
                });

            // Backfill data from legacy UserCredentials and UserIdentifiers if present
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'UserCredentials') THEN
                        INSERT INTO ""Credentials"" (""Id"", ""IdentityId"", ""CredentialTypeId"", ""CredentialData"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                        SELECT gen_random_uuid(), uc.""UserId"", 1, uc.""CredentialData"", NOW(), NOW()
                        FROM ""UserCredentials"" uc
                        ON CONFLICT DO NOTHING;
                    END IF;

                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'UserIdentifiers') THEN
                        INSERT INTO ""CredentialIdentifiers"" (""Id"", ""IdentityId"", ""CredentialId"", ""Type"", ""Value"", ""IsPrimary"", ""CreatedAtUtc"")
                        SELECT gen_random_uuid(), ui.""UserId"", c.""Id"", ui.""Type"", ui.""Value"", true, NOW()
                        FROM ""UserIdentifiers"" ui
                        LEFT JOIN ""Credentials"" c ON c.""IdentityId"" = ui.""UserId"" AND c.""CredentialTypeId"" = 1
                        ON CONFLICT DO NOTHING;
                    END IF;
                END $$;
            ");

            migrationBuilder.DropTable(
                name: "UserCredentials");

            migrationBuilder.DropTable(
                name: "UserIdentifiers");

            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW ""Users"" AS SELECT * FROM ""Identities"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_CredentialIdentifiers_CredentialId",
                table: "CredentialIdentifiers",
                column: "CredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_CredentialIdentifiers_IdentityId",
                table: "CredentialIdentifiers",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_CredentialIdentifiers_Type_Value",
                table: "CredentialIdentifiers",
                columns: new[] { "Type", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_CredentialTypeId",
                table: "Credentials",
                column: "CredentialTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_IdentityId",
                table: "Credentials",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_CredentialTypes_Code",
                table: "CredentialTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS ""Users"";");

            migrationBuilder.DropTable(
                name: "CredentialIdentifiers");

            migrationBuilder.DropTable(
                name: "Credentials");

            migrationBuilder.DropTable(
                name: "CredentialTypes");

            migrationBuilder.RenameTable(
                name: "Identities",
                newName: "Users");

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
        }
    }
}
