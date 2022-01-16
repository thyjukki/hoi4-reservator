using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Reservator.Models;

namespace Reservator.Modules
{
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly GameContext _database;

        public AdminModule(GameContext database)
        {
            _database = database;
        }

        [Command("addpermission")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public Task AddPermissionToRole([Summary("Role to give permission")] IRole role, [Remainder] string permission)
        {
            if (permission != "admin" && permission != "deny" && permission != "restricted")
                return ReplyAsync("Allowed permissions are admin,  deny and restricted");
            if (_database.GuildRoles.ToList().Exists(_ =>
                _.GuildId == Context.Guild.Id && _.RoleId == role.Id && _.Permission == permission))
            {
                return ReplyAsync("Role has already been assigned {permission}");
            }

            _database.GuildRoles.Add(new GuildRoles
                { GuildId = Context.Guild.Id, RoleId = role.Id, Permission = permission });
            _database.SaveChanges();
            return ReplyAsync($"Role added as {permission}");
        }


        [Command("removepermission")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public Task RemovePermissionFromRole([Summary("Role to remove permission from")] IRole role,
            [Remainder] string permission)
        {
            if (permission != "admin" && permission != "deny" && permission != "restricted")
                return ReplyAsync("Allowed permissions are admin,  deny and restricted");

            var guildRole = _database.GuildRoles.FirstOrDefault(guildRole =>
                guildRole.GuildId == Context.Guild.Id && guildRole.RoleId == role.Id &&
                guildRole.Permission == permission);

            if (guildRole == null) return ReplyAsync("Role has not been assigned the permission");

            _database.GuildRoles.Remove(guildRole);
            _database.SaveChanges();
            return ReplyAsync("Permission removed from role");
        }
    }
}