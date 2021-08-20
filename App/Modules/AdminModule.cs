using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Reservator.Models;
using Reservator.Preconditions;
using Reservator.Services;

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
            foreach (var game in _database.Games)
            {
                if (toChannel.Id != game.ChannelId && Context.Guild.Id != game.GuildId) continue;
                var oldReservationMessage = await Context.Channel.GetMessageAsync(game.ReservationMessageId);
                var oldReactionAlliesMessage = await Context.Channel.GetMessageAsync(game.ReactionsAlliesMessageId);
                var oldReactionAxisMessage = await Context.Channel.GetMessageAsync(game.ReactionsAxisMessageId);
                oldReservationMessage?.DeleteAsync();
                oldReactionAlliesMessage?.DeleteAsync();
                oldReactionAxisMessage?.DeleteAsync();

                game.Reservations?.Clear();
                _database.Games.Remove(game);
            }

            IUserMessage replyReservations;
            IUserMessage replyReactionsAllies;
            IUserMessage replyReactionsAxis;
            IUserMessage replyReactionsOther;
            if (channel == null)
            {
                replyReservations = await ReplyAsync("__**Current reservations:**__");
                replyReactionsAllies = await ReplyAsync("Click on reaction to reserver/unreserve\nAllies:");
                replyReactionsAxis = await ReplyAsync("Axis:");
                replyReactionsOther = await ReplyAsync("✋ Will show up (new players can use this)\n❌ Cancel reservation:");
            }
            else
            {
                replyReservations = await Context.Guild.GetTextChannel(channel.Id).SendMessageAsync("__**Current reservations:**__");
                replyReactionsAllies = await Context.Guild.GetTextChannel(channel.Id).SendMessageAsync("Click on reaction to reserver/unreserve\nAllies:");
                replyReactionsAxis = await Context.Guild.GetTextChannel(channel.Id).SendMessageAsync("Axis:");
                replyReactionsOther = await Context.Guild.GetTextChannel(channel.Id).SendMessageAsync("✋ Will show up (new players can use this)\n❌ Cancel reservation:");
            }

            _database.Add(new Models.Game
            {
                ReservationMessageId = replyReservations.Id,
                ReactionsAlliesMessageId = replyReactionsAllies.Id,
                ReactionsAxisMessageId = replyReactionsAxis.Id,
                ReactionsOtherMessageId = replyReactionsOther.Id,
                ChannelId = toChannel.Id,
                GuildId = Context.Guild.Id
            });
            await _database.SaveChangesAsync();

            await replyReactionsAllies.AddReactionsAsync(_countryConfigs.CountryConfig.Countries
                .Where(_ => Utilities.IsInFaction(_countryConfigs, _.Name, "allies") ||
                            Utilities.IsInFaction(_countryConfigs, _.Name, "comitern") ||
                            Utilities.IsInFaction(_countryConfigs, _.Name, "unaligned"))
                .Select(country => new Emoji(country.Emoji))
                .Cast<IEmote>().ToArray());
            await replyReactionsAxis.AddReactionsAsync(_countryConfigs.CountryConfig.Countries
                .Where(_ => Utilities.IsInFaction(_countryConfigs, _.Name, "axis") ||
                            Utilities.IsInFaction(_countryConfigs, _.Name, "copro"))
                .Select(country => new Emoji(country.Emoji))
                .Cast<IEmote>().ToArray());
            await replyReactionsOther.AddReactionAsync(new Emoji("✋"));
            await replyReactionsOther.AddReactionAsync(new Emoji("❌"));
        }

        [Command("removegame")]
        [RequireManager]
        public async Task RemoveGame(IChannel channel = null)
        {
            var toChannel = channel ?? Context.Channel;
            foreach (var game in _database.Games)
            {
                if (toChannel.Id != game.ChannelId && Context.Guild.Id != game.GuildId) continue;
                var oldReservationMessage = await Context.Channel.GetMessageAsync(game.ReservationMessageId);
                var oldReactionAlliesMessage = await Context.Channel.GetMessageAsync(game.ReactionsAlliesMessageId);
                var oldReactionAxisMessage = await Context.Channel.GetMessageAsync(game.ReactionsAxisMessageId);
                oldReservationMessage?.DeleteAsync();
                oldReactionAlliesMessage?.DeleteAsync();
                oldReactionAxisMessage?.DeleteAsync();

                game.Reservations.Clear();
                _database.Games.Remove(game);
            }

            await _database.SaveChangesAsync();
        }

        [Command("addmanagerole")]
        [Summary("Add roles that can create reservations")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public Task AddRoleAllowed([Remainder] [Summary("Role to give rights to setup games")] IRole role)
        {
            if (_database.GuildRoles.ToList().Exists(_ => _.GuildId == Context.Guild.Id && _.RoleId == role.Id && _.Type == "admin"))
            {
                return ReplyAsync("Role has already been assigned admin");
            }

            _database.GuildRoles.Add(new GuildRoles { GuildId = Context.Guild.Id, RoleId = role.Id, Type = "admin" });
            _database.SaveChanges();
            return ReplyAsync("Role added as manager");
        }

        [Command("adddenyrole")]
        [Summary("Add roles that can not reserve games")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public Task AddRoleDeny([Remainder] [Summary("Role to give deny right to reserve")] IRole role)
        {
            if (_database.GuildRoles.ToList().Exists(_ => _.GuildId == Context.Guild.Id && _.RoleId == role.Id && _.Type == "deny"))
            {
                return ReplyAsync("Role has already been denied reserving");
            }

            _database.GuildRoles.Add(new GuildRoles { GuildId = Context.Guild.Id, RoleId = role.Id, Type = "deny" });
            _database.SaveChanges();
            return ReplyAsync("Role marked as dennied");
        }

        [Command("addrestrictedrole")]
        [Summary("Add roles that can only show up")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public Task AddRoleRestricted([Remainder] [Summary("Role to give only showup right")] IRole role)
        {
            if (_database.GuildRoles.ToList().Exists(_ => _.GuildId == Context.Guild.Id && _.RoleId == role.Id && _.Type == "restricted"))
            {
                return ReplyAsync("Role has already been restricted");
            }

            _database.GuildRoles.Add(new GuildRoles { GuildId = Context.Guild.Id, RoleId = role.Id, Type = "restricted" });
            _database.SaveChanges();
            return ReplyAsync("Role marked as restricted");
        }

        [Command("removemanagerole")]
        [Summary("Remove roles that can create reservations")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public Task RemoveRoleAllowed([Remainder] [Summary("Role to remove rights to setup games")] IRole role)
        {
            foreach (var guildRole in _database.GuildRoles)
            {
                if (guildRole.GuildId != Context.Guild.Id || guildRole.RoleId != role.Id ||
                    guildRole.Type != "admin") continue;
                _database.GuildRoles.Remove(guildRole);
                _database.SaveChanges();
                return ReplyAsync("Role removed from being a manager");
            }
            
            return ReplyAsync("Role has not been assigned as manager");
        }

        [Command("removedenyrole")]
        [Summary("Add roles that can not reserve games")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public Task RemoveRoleDeny([Remainder] [Summary("Role to remove denied rights to reserve")] IRole role)
        {
            foreach (var guildRole in _database.GuildRoles)
            {
                if (guildRole.GuildId != Context.Guild.Id || guildRole.RoleId != role.Id ||
                    guildRole.Type != "deny") continue;
                _database.GuildRoles.Remove(guildRole);
                _database.SaveChanges();
                return ReplyAsync("Role removed from being denied");
            }
            
            return ReplyAsync("Role has not been assigned as denied");
        }

        [Command("removerestrictedrole")]
        [Summary("Add roles that can only show up")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public Task RemoveRoleRestricted([Remainder] [Summary("Role to remove restricted reserve")] IRole role)
        {
            foreach (var guildRole in _database.GuildRoles)
            {
                if (guildRole.GuildId != Context.Guild.Id || guildRole.RoleId != role.Id ||
                    guildRole.Type != "restricted") continue;
                _database.GuildRoles.Remove(guildRole);
                _database.SaveChanges();
                return ReplyAsync("Role removed from being restricted");
            }
            
            return ReplyAsync("Role has not been assigned as restricted");
        }
    }
}