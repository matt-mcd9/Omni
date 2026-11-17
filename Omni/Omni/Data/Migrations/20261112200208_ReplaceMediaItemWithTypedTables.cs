using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Omni.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMediaItemWithTypedTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favourites_MediaItems_MediaId",
                table: "Favourites");

            migrationBuilder.DropForeignKey(
                name: "FK_LaunchHistory_MediaItems_MediaId",
                table: "LaunchHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistItems_MediaItems_MediaId",
                table: "PlaylistItems");

            migrationBuilder.DropTable(
                name: "MediaItems");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistItems_MediaId",
                table: "PlaylistItems");

            migrationBuilder.DropIndex(
                name: "IX_LaunchHistory_MediaId",
                table: "LaunchHistory");

            migrationBuilder.DropIndex(
                name: "IX_Favourites_MediaId",
                table: "Favourites");

            migrationBuilder.DropIndex(
                name: "IX_Favourites_UserId_MediaId",
                table: "Favourites");

            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "Favourites",
                newName: "ItemId");

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "PlaylistItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "PlaylistItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "LaunchHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "LaunchHistory",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "Favourites",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    Game_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    User_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Game_id);
                    table.ForeignKey(
                        name: "FK_Game_UserProfiles_User_id",
                        column: x => x.User_id,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Music",
                columns: table => new
                {
                    Music_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    User_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Music", x => x.Music_id);
                    table.ForeignKey(
                        name: "FK_Music_UserProfiles_User_id",
                        column: x => x.User_id,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Software",
                columns: table => new
                {
                    Software_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    User_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Software", x => x.Software_id);
                    table.ForeignKey(
                        name: "FK_Software_UserProfiles_User_id",
                        column: x => x.User_id,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Video",
                columns: table => new
                {
                    Video_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    User_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Video", x => x.Video_id);
                    table.ForeignKey(
                        name: "FK_Video_UserProfiles_User_id",
                        column: x => x.User_id,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favourites_UserId_MediaType_ItemId",
                table: "Favourites",
                columns: new[] { "UserId", "MediaType", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Game_User_id",
                table: "Game",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_Music_User_id",
                table: "Music",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_Software_User_id",
                table: "Software",
                column: "User_id");

            migrationBuilder.CreateIndex(
                name: "IX_Video_User_id",
                table: "Video",
                column: "User_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Game");

            migrationBuilder.DropTable(
                name: "Music");

            migrationBuilder.DropTable(
                name: "Software");

            migrationBuilder.DropTable(
                name: "Video");

            migrationBuilder.DropIndex(
                name: "IX_Favourites_UserId_MediaType_ItemId",
                table: "Favourites");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "LaunchHistory");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "LaunchHistory");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Favourites");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "Favourites",
                newName: "MediaId");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    MediaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    MediaType = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.MediaId);
                    table.ForeignKey(
                        name: "FK_MediaItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MediaItems_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName" },
                values: new object[,]
                {
                    { 1, "Music" },
                    { 2, "Video" },
                    { 3, "Game" },
                    { 4, "Software" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistItems_MediaId",
                table: "PlaylistItems",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_LaunchHistory_MediaId",
                table: "LaunchHistory",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Favourites_MediaId",
                table: "Favourites",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Favourites_UserId_MediaId",
                table: "Favourites",
                columns: new[] { "UserId", "MediaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_CategoryId",
                table: "MediaItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_UserId",
                table: "MediaItems",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Favourites_MediaItems_MediaId",
                table: "Favourites",
                column: "MediaId",
                principalTable: "MediaItems",
                principalColumn: "MediaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LaunchHistory_MediaItems_MediaId",
                table: "LaunchHistory",
                column: "MediaId",
                principalTable: "MediaItems",
                principalColumn: "MediaId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistItems_MediaItems_MediaId",
                table: "PlaylistItems",
                column: "MediaId",
                principalTable: "MediaItems",
                principalColumn: "MediaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
