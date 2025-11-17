using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialMediaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedInOrganizationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "SocialAccounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "SocialAccounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountType",
                table: "SocialAccounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Personal");

            migrationBuilder.AddColumn<string>(
                name: "Scopes",
                table: "SocialAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalData",
                table: "SocialAccounts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "SocialAccounts");

            migrationBuilder.DropColumn(
                name: "AdditionalData",
                table: "SocialAccounts");
        }
    }
}
