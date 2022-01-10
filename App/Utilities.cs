using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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
    }
}