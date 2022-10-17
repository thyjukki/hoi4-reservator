using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reservator.Models;
using Game = Reservator.Models.Game;

namespace Reservator.Services;

public class ReactionHandlingService
{
    private readonly DiscordSocketClient _discord;
    private readonly CountryConfigService _countryConfigService;
    private readonly GameContext _database;

    public ReactionHandlingService(IServiceProvider services)
    {
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _countryConfigService = services.GetRequiredService<CountryConfigService>();
        _database = services.GetRequiredService<GameContext>();

        // Hook MessageReceived so we can process each message to see
        // if it qualifies as a command.
        _discord.ReactionAdded += ReactionAddedAsync;
    }

    private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> originChannel, SocketReaction reaction)
    {
        var message = await cachedMessage.GetOrDownloadAsync();
        if (message is not { Source: MessageSource.Bot }) return;

        var channel = (SocketTextChannel)await originChannel.GetOrDownloadAsync();
        var gUser = (SocketGuildUser)reaction.User.Value;
        if (channel != null && gUser != null && reaction.User.IsSpecified &&
            reaction.User.Value != message.Author)
        {
            var game = _database.Games.Include(b => b.Reservations).FirstOrDefault(_ => _.GuildId == channel.Guild.Id && (
                _.ReactionsAlliesMessageId == message.Id || _.ReactionsAxisMessageId == message.Id ||
                _.ReactionsOtherMessageId == message.Id));

            if (game == null) return;


            await HandleGameReaction(reaction, channel, game, gUser, message);
        }
    }

    private async Task HandleGameReaction(SocketReaction reaction, SocketTextChannel channel, Game game,
        SocketGuildUser gUser, IMessage message)
    {

        var hasDeniedRole = Utilities.HasRole(channel, _database, gUser, "deny");
        var hasRestrictedRole = Utilities.HasRole(channel, _database, gUser, "restricted");
        var hasMajorRole = Utilities.HasRole(channel, _database, gUser, "major");

        var country = Utilities.GetCountryFromEmote(_countryConfigService, reaction.Emote);
        if (reaction.Emote.Name is not ("✋" or "❌") && country == null) return;

        var oldReservations = game.Reservations.Where(reservation => reservation.User == reaction.UserId);
        foreach (var oldReservation in oldReservations)
        {
            _database.Reservations.Remove(oldReservation);
        }

        if (country != null && !hasRestrictedRole && !hasDeniedRole)
        {
            if (hasMajorRole || !country.Major)
            {
                _database.Reservations.Add(new Reservation
                    { Country = country.Name, Game = game, User = reaction.UserId });
            }
        }
        else if (reaction.Emote.Name == "✋" && !hasDeniedRole)
        {
            _database.Reservations.Add(new Reservation
                { Country = null, Game = game, User = reaction.UserId });
        }

        await _database.SaveChangesAsync();

        var content = await Utilities.BuildReservationMessage(_countryConfigService, _discord, game);
            
        var reservationsMessage = await Utilities.GetMessageIfCached(channel, game.ReservationMessageId);
        if (reservationsMessage != null)
        {
            await ((IUserMessage)reservationsMessage).ModifyAsync(x => { x.Content = content; });
        }
            
        await message.RemoveReactionAsync(reaction.Emote, reaction.UserId);
    }

}