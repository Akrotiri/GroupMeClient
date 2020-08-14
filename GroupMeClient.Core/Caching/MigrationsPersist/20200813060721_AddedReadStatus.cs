﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace GroupMeClient.Core.Caching.MigrationsPersist
{
    public partial class AddedReadStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupChatStates",
                columns: table => new
                {
                    GroupOrChatId = table.Column<string>(nullable: false),
                    LastTotalMessageCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupChatStates", x => x.GroupOrChatId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupChatStates");
        }
    }
}
