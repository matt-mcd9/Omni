using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Omni.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMediaItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old rows point at MediaItems, which is being dropped. A folder scan refills the library.
            migrationBuilder.Sql("DELETE FROM LaunchHistory; DELETE FROM Favourites; DELETE FROM PlaylistItems;");

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

            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "PlaylistItems",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistItems_MediaId",
                table: "PlaylistItems",
                newName: "IX_PlaylistItems_CategoryId");

            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "LaunchHistory",
                newName: "PlaylistItemId");

            migrationBuilder.RenameIndex(
                name: "IX_LaunchHistory_MediaId",
                table: "LaunchHistory",
                newName: "IX_LaunchHistory_PlaylistItemId");

            migrationBuilder.RenameColumn(
                name: "MediaId",
                table: "Favourites",
                newName: "PlaylistItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Favourites_UserId_MediaId",
                table: "Favourites",
                newName: "IX_Favourites_UserId_PlaylistItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Favourites_MediaId",
                table: "Favourites",
                newName: "IX_Favourites_PlaylistItemId");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "PlaylistItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "PlaylistItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "PlaylistItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Favourites_PlaylistItems_PlaylistItemId",
                table: "Favourites",
                column: "PlaylistItemId",
                principalTable: "PlaylistItems",
                principalColumn: "PlaylistItemId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LaunchHistory_PlaylistItems_PlaylistItemId",
                table: "LaunchHistory",
                column: "PlaylistItemId",
                principalTable: "PlaylistItems",
                principalColumn: "PlaylistItemId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistItems_Categories_CategoryId",
                table: "PlaylistItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favourites_PlaylistItems_PlaylistItemId",
                table: "Favourites");

            migrationBuilder.DropForeignKey(
                name: "FK_LaunchHistory_PlaylistItems_PlaylistItemId",
                table: "LaunchHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistItems_Categories_CategoryId",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "PlaylistItems");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "PlaylistItems");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "PlaylistItems",
                newName: "MediaId");

            migrationBuilder.RenameIndex(
                name: "IX_PlaylistItems_CategoryId",
                table: "PlaylistItems",
                newName: "IX_PlaylistItems_MediaId");

            migrationBuilder.RenameColumn(
                name: "PlaylistItemId",
                table: "LaunchHistory",
                newName: "MediaId");

            migrationBuilder.RenameIndex(
                name: "IX_LaunchHistory_PlaylistItemId",
                table: "LaunchHistory",
                newName: "IX_LaunchHistory_MediaId");

            migrationBuilder.RenameColumn(
                name: "PlaylistItemId",
                table: "Favourites",
                newName: "MediaId");

            migrationBuilder.RenameIndex(
                name: "IX_Favourites_UserId_PlaylistItemId",
                table: "Favourites",
                newName: "IX_Favourites_UserId_MediaId");

            migrationBuilder.RenameIndex(
                name: "IX_Favourites_PlaylistItemId",
                table: "Favourites",
                newName: "IX_Favourites_MediaId");

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
