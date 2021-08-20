using Microsoft.EntityFrameworkCore.Migrations;

namespace Reservator.Migrations
{
    public partial class GuildROlesTypeAsKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GuildRoles",
                table: "GuildRoles");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "GuildRoles",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuildRoles",
                table: "GuildRoles",
                columns: new[] { "GuildId", "RoleId", "Type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GuildRoles",
                table: "GuildRoles");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "GuildRoles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuildRoles",
                table: "GuildRoles",
                columns: new[] { "GuildId", "RoleId" });
        }
    }
}
