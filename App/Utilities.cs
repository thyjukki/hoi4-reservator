using System.Linq;
using Discord;
using Reservator.Services;

namespace Reservator
{
    public static class Utilities
    {
        public static bool IsInFaction(CountryConfigService ccs, string country, string faction) =>
            ccs.CountryConfig.Countries.Exists(_ => _.Name == country && _.Side == faction);

        public static CountryConfig GetCountryFromEmote(CountryConfigService ccs, IEmote emote) =>
            ccs.CountryConfig.Countries.FirstOrDefault(_ => _.Emoji == emote.Name);
    }
}