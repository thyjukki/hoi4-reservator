using System.Linq;
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
        
        public static string BuildReservationMessage(CountryConfigService ccs, DiscordSocketClient discord = null, Game game = null)
        {
            var message = $"__**Current reservations ({game?.Reservations.Count ?? 0}):**__";

            var faction = "";
            foreach (var country in ccs.CountryConfig.Countries)
            {
                if (country.Side != faction)
                {
                    var playerCount = game?.Reservations.Count(_ => IsInFaction(ccs, _.Country, country.Side)) ?? 0;

                    message += $"\n\n**{country.Side} ({playerCount})**";
                    faction = country.Side;
                }

                message += $"\n{country.Emoji} {country.Name}: ";

                var reservations = game?.Reservations.Where(_ => _.Country == country.Name).ToList();
                if (discord == null || reservations is not { Count: > 0 }) continue;

                message += "**";
                message += string.Join("' ",
                    reservations.Select(_ => discord.GetUser(_.User)).Select(_ => _.Mention));
                message += "**";
            }

            var showUp = game?.Reservations.Where(_ => _.Country == null).ToList();
            message += $"\n\n**Will show up ({showUp?.Count ?? 0})**";

            if (discord != null && showUp != null)
            {
                message += string.Join("\n",
                    showUp.Select(_ => discord.GetUser(_.User)).Select(_ => _.Mention));
            }

            return message;
        }
    }
}