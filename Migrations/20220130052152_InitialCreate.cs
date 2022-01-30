using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace rhuModBot.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    key = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    id = table.Column<string>(type: "TEXT", nullable: true),
                    time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    linkedArticle = table.Column<string>(type: "TEXT", nullable: true),
                    postTitle = table.Column<string>(type: "TEXT", nullable: true),
                    articleTitle = table.Column<string>(type: "TEXT", nullable: true),
                    similarity = table.Column<double>(type: "REAL", nullable: false),
                    userIsIgnored = table.Column<bool>(type: "INTEGER", nullable: false),
                    domainIsIgnored = table.Column<bool>(type: "INTEGER", nullable: false),
                    isReported = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.key);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Posts");
        }
    }
}
