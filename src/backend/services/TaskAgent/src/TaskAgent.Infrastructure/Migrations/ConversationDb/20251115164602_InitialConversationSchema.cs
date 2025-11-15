using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskAgent.Infrastructure.Migrations.ConversationDb
{
    /// <inheritdoc />
    public partial class InitialConversationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationThreads",
                columns: table => new
                {
                    ThreadId = table.Column<string>(
                        type: "varchar(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    SerializedThread = table.Column<string>(type: "json", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    MessageCount = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        defaultValue: 0
                    ),
                    IsActive = table.Column<bool>(
                        type: "boolean",
                        nullable: false,
                        defaultValue: true
                    ),
                    Title = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    Preview = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationThreads", x => x.ThreadId);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ConversationThreads_CreatedAt",
                table: "ConversationThreads",
                column: "CreatedAt",
                descending: []
            );

            migrationBuilder.CreateIndex(
                name: "IX_ConversationThreads_IsActive",
                table: "ConversationThreads",
                column: "IsActive"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ConversationThreads_UpdatedAt",
                table: "ConversationThreads",
                column: "UpdatedAt",
                descending: []
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ConversationThreads");
        }
    }
}
