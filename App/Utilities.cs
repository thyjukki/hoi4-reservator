using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Reservator.Models;
using Reservator.Services;
using Game = Reservator.Models.Game;

namespace Reservator
{
    public static class Utilities
    {
        public static bool IsInFaction(CountryConfigService ccs, string country, string faction) =>
            ccs.CountryConfig.Countries.Exists(_ => _.Name == country && _.Side == faction);

        public static CountryConfig GetCountryFromEmote(CountryConfigService ccs, IEmote emote) =>
            ccs.CountryConfig.Countries.FirstOrDefault(_ => _.Emoji == emote.Name);
        
        public static async Task<string> BuildReservationMessage(CountryConfigService ccs, DiscordSocketClient discord = null, Game game = null)
        {
            var bld = new StringBuilder();

            bld.Append($"__**Current reservations ({game?.Reservations.Count ?? 0}):**__");

            var faction = "";
            foreach (var country in ccs.CountryConfig.Countries)
            {
                if (country.Side != faction)
                {
                    var playerCount = game?.Reservations.Count(_ => IsInFaction(ccs, _.Country, country.Side)) ?? 0;

                    bld.Append($"\n\n**{country.Side} ({playerCount})**");
                    faction = country.Side;
                }

                bld.Append($"\n{country.Emoji} {country.Name}: ");

                var reservations = game?.Reservations.Where(_ => _.Country == country.Name).ToList();
                if (discord == null || reservations is not { Count: > 0 }) continue;

                var userNames = new List<string>();
                foreach (var reservation in reservations)
                {
                    var user = discord.GetUser(reservation.User) ?? await discord.GetUserAsync(reservation.User);
                    userNames.Add(user == null ? "Unknown user" : user.Mention);
                }
                bld.Append($"**{string.Join("' ",userNames)}**");
            }

            var showUp = game?.Reservations.Where(_ => _.Country == null).ToList();
            bld.Append($"\n\n**Will show up ({showUp?.Count ?? 0})**");

            if (discord == null || showUp == null) return bld.ToString();

            var showUpUserNames = new List<string>();
            foreach (var reservation in showUp)
            {
                var user = discord.GetUser(reservation.User) ?? await discord.GetUserAsync(reservation.User);
                showUpUserNames.Add(user == null ? "Unknown user" : user.Mention);
            }
            bld.Append(string.Join("\n", showUpUserNames));

            return bld.ToString();
        }
        
        
        public static async Task ClearOldGames(IMessageChannel toChannel, IGuild guild, GameContext database)
        {
            var games = database.Games.Include(b => b.Reservations).ToList().Where(game => toChannel.Id == game.ChannelId && guild.Id == game.GuildId);
            foreach (var game in games)
            {
                var oldReservationMessage = await toChannel.GetMessageAsync(game.ReservationMessageId);
                var oldReactionAlliesMessage = await toChannel.GetMessageAsync(game.ReactionsAlliesMessageId);
                var oldReactionAxisMessage = await toChannel.GetMessageAsync(game.ReactionsAxisMessageId);
                var oldReactionOtherMessage = await toChannel.GetMessageAsync(game.ReactionsOtherMessageId);
                oldReservationMessage?.DeleteAsync();
                oldReactionAlliesMessage?.DeleteAsync();
                oldReactionAxisMessage?.DeleteAsync();
                oldReactionOtherMessage?.DeleteAsync();

                foreach (var reservation in game.Reservations.ToList())
                    database.Reservations.Remove(reservation);
                database.Games.Remove(game);
            }
        }

        public static bool HasRole(SocketGuildChannel channel, GameContext  database, SocketGuildUser gUser, string role)
        {
            var hasDeniedRole = database.GuildRoles.ToList().Exists(_ =>
                channel.Guild.Id == _.GuildId && gUser.Roles.Any(r => r.Id == _.RoleId) && _.Permission == role);
            return hasDeniedRole;
        }
        
        

        public static async Task<IMessage> GetMessageIfCached(SocketTextChannel channel,
            ulong gameReactionsAlliesMessageId) =>
            channel.GetCachedMessage(gameReactionsAlliesMessageId) ??
            await channel.GetMessageAsync(gameReactionsAlliesMessageId);
    }
}