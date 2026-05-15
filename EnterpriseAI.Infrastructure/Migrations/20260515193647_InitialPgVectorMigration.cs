using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace EnterpriseAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPgVectorMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "KnowledgeBase",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationCode = table.Column<string>(type: "text", nullable: false),
                    ModuleCode = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    PageNumber = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    Metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    IsImageDescription = table.Column<bool>(type: "boolean", nullable: false),
                    SourceDocument = table.Column<string>(type: "text", nullable: false),
                    FileHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBase", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBase_ApplicationCode",
                table: "KnowledgeBase",
                column: "ApplicationCode");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBase_FileHash",
                table: "KnowledgeBase",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBase_PageNumber",
                table: "KnowledgeBase",
                column: "PageNumber");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBase_SourceDocument",
                table: "KnowledgeBase",
                column: "SourceDocument");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeBase");
        }
    }
}

