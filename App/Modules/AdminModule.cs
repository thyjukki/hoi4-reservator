using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Reservator.Models;
using Reservator.Preconditions;
using Reservator.Services;
using Game = Reservator.Models.Game;

namespace Reservator.Modules
{
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly GameContext _database;
        private readonly CountryConfigService _countryConfigs;

        public AdminModule(GameContext database, CountryConfigService countryConfig)
        {
            _database = database;
            _countryConfigs = countryConfig;
        }

        [Command("newgame")]
        [RequireManager]
        public async Task NewGame(IChannel channel = null)
        {
            var toChannel = channel ?? Context.Channel;
            await ClearOldGames(toChannel);

            var userMessages = await Utilities.BuildReservationMessage(_countryConfigs);
            var replyReservations = await Context.Guild.GetTextChannel(toChannel.Id)
                .SendMessageAsync(userMessages);
            var replyReactionsAllies = await Context.Guild.GetTextChannel(toChannel.Id)
                .SendMessageAsync("Click on reaction to reserve/unreserve\nAllies:");
            var replyReactionsAxis = await Context.Guild.GetTextChannel(toChannel.Id).SendMessageAsync("Axis:");
            var replyReactionsOther = await Context.Guild.GetTextChannel(toChannel.Id)
                .SendMessageAsync("✋ Will show up (new players can use this)\n❌ Cancel reservation:");

            _database.Add(new Game
            {
                ReservationMessageId = replyReservations.Id,
                ReactionsAlliesMessageId = replyReactionsAllies.Id,
                ReactionsAxisMessageId = replyReactionsAxis.Id,
                ReactionsOtherMessageId = replyReactionsOther.Id,
                ChannelId = toChannel.Id,
                GuildId = Context.Guild.Id
            });
            await _database.SaveChangesAsync();

            _ = replyReactionsAllies.AddReactionsAsync(_countryConfigs.CountryConfig.Countries
                .Where(_ => Utilities.IsInFaction(_countryConfigs, _.Name, "allies") ||
                            Utilities.IsInFaction(_countryConfigs, _.Name, "comitern") ||
                            Utilities.IsInFaction(_countryConfigs, _.Name, "unaligned"))
                .Select(country => new Emoji(country.Emoji))
                .Cast<IEmote>().ToArray());
            _ = replyReactionsAxis.AddReactionsAsync(_countryConfigs.CountryConfig.Countries
                .Where(_ => Utilities.IsInFaction(_countryConfigs, _.Name, "axis") ||
                            Utilities.IsInFaction(_countryConfigs, _.Name, "copro"))
                .Select(country => new Emoji(country.Emoji))
                .Cast<IEmote>().ToArray());
            _ = replyReactionsOther.AddReactionAsync(new Emoji("✋"))
                .ContinueWith(_ => replyReactionsOther.AddReactionAsync(new Emoji("❌")));
        }

        [Command("removegame")]
        [RequireManager]
        public async Task RemoveGame(IChannel channel = null)
        {
            var toChannel = channel ?? Context.Channel;
            await ClearOldGames(toChannel);

            await _database.SaveChangesAsync();
        }

        private async Task ClearOldGames(IChannel toChannel)
        {
            var games = _database.Games.Include(b => b.Reservations).ToList().Where(game => toChannel.Id == game.ChannelId && Context.Guild.Id == game.GuildId);
            foreach (var game in games)
            {
                var oldReservationMessage = await Context.Channel.GetMessageAsync(game.ReservationMessageId);
                var oldReactionAlliesMessage = await Context.Channel.GetMessageAsync(game.ReactionsAlliesMessageId);
                var oldReactionAxisMessage = await Context.Channel.GetMessageAsync(game.ReactionsAxisMessageId);
                var oldReactionOtherMessage = await Context.Channel.GetMessageAsync(game.ReactionsOtherMessageId);
                oldReservationMessage?.DeleteAsync();
                oldReactionAlliesMessage?.DeleteAsync();
                oldReactionAxisMessage?.DeleteAsync();
                oldReactionOtherMessage?.DeleteAsync();

                foreach (var reservation in game.Reservations.ToList())
                    _database.Reservations.Remove(reservation);
                _database.Games.Remove(game);
            }
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