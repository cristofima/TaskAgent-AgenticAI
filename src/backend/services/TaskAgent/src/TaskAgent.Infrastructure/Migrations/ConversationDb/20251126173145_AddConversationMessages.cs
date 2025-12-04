using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskAgent.Infrastructure.Migrations.ConversationDb
{
    /// <inheritdoc />
    public partial class AddConversationMessages : Migration
    {
        private static readonly string[] IsActiveUpdatedAtColumns = ["IsActive", "UpdatedAt"];
        private static readonly string[] ThreadIdTimestampColumns = ["ThreadId", "Timestamp"];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConversationThreads_CreatedAt",
                table: "ConversationThreads");

            migrationBuilder.DropIndex(
                name: "IX_ConversationThreads_UpdatedAt",
                table: "ConversationThreads");

            migrationBuilder.DropColumn(
                name: "SerializedThread",
                table: "ConversationThreads");

            migrationBuilder.AlterColumn<int>(
                name: "MessageCount",
                table: "ConversationThreads",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ConversationThreads",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "ThreadId",
                table: "ConversationThreads",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "SerializedState",
                table: "ConversationThreads",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConversationMessages",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ThreadId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "json", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMessages", x => x.MessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationThreads_IsActive_UpdatedAt",
                table: "ConversationThreads",
                columns: IsActiveUpdatedAtColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationThreads_UpdatedAt",
                table: "ConversationThreads",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ThreadId",
                table: "ConversationMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ThreadId_Timestamp",
                table: "ConversationMessages",
                columns: ThreadIdTimestampColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationMessages");

            migrationBuilder.DropIndex(
                name: "IX_ConversationThreads_IsActive_UpdatedAt",
                table: "ConversationThreads");

            migrationBuilder.DropIndex(
                name: "IX_ConversationThreads_UpdatedAt",
                table: "ConversationThreads");

            migrationBuilder.DropColumn(
                name: "SerializedState",
                table: "ConversationThreads");

            migrationBuilder.AlterColumn<int>(
                name: "MessageCount",
                table: "ConversationThreads",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "ConversationThreads",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "ThreadId",
                table: "ConversationThreads",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "SerializedThread",
                table: "ConversationThreads",
                type: "json",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationThreads_CreatedAt",
                table: "ConversationThreads",
                column: "CreatedAt",
                descending: Array.Empty<bool>());

            migrationBuilder.CreateIndex(
                name: "IX_ConversationThreads_UpdatedAt",
                table: "ConversationThreads",
                column: "UpdatedAt",
                descending: Array.Empty<bool>());
        }
    }
}
