using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialMediaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookPageSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PageId",
                table: "SocialAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageAccessToken",
                table: "SocialAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageName",
                table: "SocialAccounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPageAccount",
                table: "SocialAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "SocialAccounts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PageId",
                table: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "PageAccessToken",
                table: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "PageName",
                table: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "IsPageAccount",
                table: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "SocialAccounts");
        }
    }
}
