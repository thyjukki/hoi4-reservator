using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Reservator.Models;
using Game = Reservator.Models.Game;

namespace Reservator.Services
{
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
                var game = _database.Games.FirstOrDefault(_ => _.GuildId == channel.Guild.Id && (
                    _.ReactionsAlliesMessageId == message.Id || _.ReactionsAxisMessageId == message.Id ||
                    _.ReactionsOtherMessageId == message.Id));

                if (game == null) return;


                await HandleGameReaction(reaction, channel, game, gUser);
            }
        }

        private async Task HandleGameReaction(SocketReaction reaction, SocketTextChannel channel, Game game,
            SocketGuildUser gUser)
        {
            var reactionAlliesMessage = await GetMessageIfCached(channel, game.ReactionsAlliesMessageId);
            var reactionAxisMessage = await GetMessageIfCached(channel, game.ReactionsAxisMessageId);
            var reservationOtherMessage = await GetMessageIfCached(channel, game.ReactionsOtherMessageId);
            var reservationsMessage = await GetMessageIfCached(channel, game.ReservationMessageId);

            var hasDeniedRole = HasRole(channel, gUser, "deny");
            var hasRestrictedRole = HasRole(channel, gUser, "restricted");

            var country = Utilities.GetCountryFromEmote(_countryConfigService, reaction.Emote);
            if (reaction.Emote.Name is not ("✋" or "❌") && country == null) return;

            var oldReservations = game.Reservations.Where(reservation => reservation.User == reaction.UserId);
            foreach (var oldReservation in oldReservations)
            {
                _database.Reservations.Remove(oldReservation);
            }

            if (country != null && !hasRestrictedRole && !hasDeniedRole)
            {
                _database.Reservations.Add(new Reservation
                    { Country = country.Name, Game = game, User = reaction.UserId });
            }
            else if (reaction.Emote.Name == "✋" && !hasDeniedRole)
            {
                _database.Reservations.Add(new Reservation
                    { Country = null, Game = game, User = reaction.UserId });
            }

            await _database.SaveChangesAsync();

            var content = Utilities.BuildReservationMessage(_countryConfigService, _discord, game);
            if (reservationsMessage != null)
            {
                await ((IUserMessage)reservationsMessage).ModifyAsync(x => { x.Content = content; });
            }
            
            var taskList = new List<Task>();
            await ClearFromReactions(reactionAlliesMessage, reaction, taskList);
            await ClearFromReactions(reactionAxisMessage, reaction, taskList);
            await ClearFromReactions(reservationOtherMessage, reaction, taskList);
            Task.WaitAll(taskList.ToArray());
        }

        private bool HasRole(SocketGuildChannel channel, SocketGuildUser gUser, string role)
        {
            var hasDeniedRole = _database.GuildRoles.ToList().Exists(_ =>
                channel.Guild.Id == _.GuildId && gUser.Roles.Any(r => r.Id == _.RoleId) && _.Permission == role);
            return hasDeniedRole;
        }

        private static async Task<IMessage> GetMessageIfCached(SocketTextChannel channel,
            ulong gameReactionsAlliesMessageId) =>
            channel.GetCachedMessage(gameReactionsAlliesMessageId) ??
            await channel.GetMessageAsync(gameReactionsAlliesMessageId);

        private static async Task ClearFromReactions(IMessage message, SocketReaction reaction, List<Task> taskList)
        {
            foreach (var (emote, _) in message.Reactions)
            {
                var users = await message.GetReactionUsersAsync(emote, 10).FlattenAsync();
                taskList.AddRange(from user in users
                    where user.Id == reaction.UserId
                    select message.RemoveReactionAsync(emote, reaction.UserId));
            }
        }
    }
}