using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Reservator.Models;
using Game = Reservator.Models.Game;

namespace Reservator.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;
        private readonly CountryConfigService _countryConfigs;
        private readonly GameContext _database;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _countryConfigs = services.GetRequiredService<CountryConfigService>();
            _database = services.GetRequiredService<GameContext>();
            services.GetRequiredService<GameContext>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            // Hook MessageReceived so we can process each message to see
            // if it qualifies as a command.
            _discord.MessageReceived += MessageReceivedAsync;
            _discord.Ready += ClientReady;
            _discord.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.User is not SocketGuildUser user || command.Channel is not SocketGuildChannel channel)
            {
                await command.RespondAsync("Not a guild channel");
                return;
            }

            if (!user.GuildPermissions.Administrator && !Utilities.HasRole(channel, _database, user, "admin"))
            {
                await command.RespondAsync("You don't have permission  to run this command");
                return;
            }
            // Let's add a switch statement for the command name so we can handle multiple commands in one event.
            switch(command.Data.Name)
            {
                case "new_game":
                    await HandleNewGameCommand(command);
                    break;
                case "remove_game":
                    await HandleRemoveGameCommand(command);
                    break;
                case "remove_reservation":
                    await HandleRemoveReservationCommand(command);
                    break;
                case "add_role":
                    await HandleAddRoleCommand(command);
                    break;
                case "remove_role":
                    await HandleRemoveRoleCommand(command);
                    break;
                case "list_roles":
                    await HandleListRolesCommand(command);
                    break;
            }
        }

        private async Task HandleListRolesCommand(SocketSlashCommand command)
        {
            var parameters = command.Data.Options.ToDictionary(x => x.Name, x => x.Value);
            if(!parameters.TryGetValue("role", out var roleObj))
            {
                await command.RespondAsync("No role specified");
            }
            if (command.Channel is not IGuildChannel guildChannel)
            {
                await command.RespondAsync("Need to be a guild channel");
                return;
            }
            if (roleObj is not IRole role)
            {
                await command.RespondAsync("Need to be a valid guild role");
                return;
            }

            var guildRoles = _database.GuildRoles.AsEnumerable().Where(guildRole => guildRole.GuildId == guildChannel.GuildId && guildRole.RoleId == role.Id).Select(_ => _.Permission).ToArray();

            if (guildRoles.Length == 0)
            {
                await command.RespondAsync("No permissions for given role");
                return;
            }
            
            await command.RespondAsync($"{role.Name} has following permissions {string.Join(", ", guildRoles)}");
        }
        
        private static async Task<(IRole role, string permission, IGuildChannel guildChannel)> GetRoleParameters(SocketSlashCommand command)
        {
            var parameters = command.Data.Options.ToDictionary(x => x.Name, x => x.Value);
            if (!parameters.TryGetValue("role", out var roleObj))
            {
                await command.RespondAsync("No role specified");
                return (null, null, null);
            }

            if (!parameters.TryGetValue("permission", out var permissionObj))
            {
                await command.RespondAsync("No permission specified");
                return (null, null, null);
            }

            if (roleObj is not IRole role)
            {
                await command.RespondAsync("Need to be a valid guild role");
                return (null, null, null);
            }

            var permission = permissionObj as string;
            if (command.Channel is not IGuildChannel guildChannel)
            {
                await command.RespondAsync("Need to be a guild channel");
                return (role, permission, null);
            }

            if (permission is "admin" or "deny" or "restricted" or "major")
                return (role, permission, guildChannel);
            await command.RespondAsync("Allowed permissions are admin, major deny and restricted");
            return (role, permission, guildChannel);

        }

        private async Task HandleRemoveRoleCommand(SocketSlashCommand command)
        {
            var (role, permission, guildChannel) = await GetRoleParameters(command);
            if (role == null || permission == null || guildChannel == null) return;

            var guildRole = _database.GuildRoles.FirstOrDefault(guildRole =>
                guildRole.GuildId == guildChannel.GuildId && guildRole.RoleId == role.Id &&
                guildRole.Permission == permission);

            if (guildRole == null)
            {
                await command.RespondAsync("Role has not been assigned the permission");
                return;
            }

            _database.GuildRoles.Remove(guildRole);
            _database.SaveChanges();
            await command.RespondAsync("Permission removed from role");
        }

        private async Task HandleAddRoleCommand(SocketSlashCommand command)
        {
            var (role, permission, guildChannel) = await GetRoleParameters(command);
            if (role == null || permission == null || guildChannel == null) return;

            if (_database.GuildRoles.ToList().Exists(_ =>
                    _.GuildId == guildChannel.GuildId && _.RoleId == role.Id && _.Permission == permission))
            {
                await command.RespondAsync($"Role has already been assigned {permission}");
                return;
            }

            _database.GuildRoles.Add(new GuildRoles
                { GuildId = guildChannel.GuildId, RoleId = role.Id, Permission = permission });
            _database.SaveChanges();
            await command.RespondAsync($"Role added as {permission}");
        }

        private async Task HandleRemoveReservationCommand(SocketSlashCommand command)
        {
            var parameters = command.Data.Options.ToDictionary(x => x.Name, x => x.Value);
            if(!parameters.TryGetValue("user", out var userObj))
            {
                await command.RespondAsync("No user specified");
                return;
            }

            var userToEdit = userObj as SocketGuildUser;
            ISocketMessageChannel channel = null;
            if (parameters.TryGetValue("channel", out var channelObj))
            {
                channel = channelObj as ISocketMessageChannel;
            }
            var toChannel = (channel ?? command.Channel) as SocketTextChannel;

            
            if (userToEdit == null)
            {
                await command.RespondAsync("User not found");
                return;
            }
            if (toChannel == null)
            {
                await command.RespondAsync("Channel not found");
                return;
            }
            
            
            var game = _database.Games.Include(b => b.Reservations).FirstOrDefault(_ => _.GuildId == toChannel.Guild.Id && _.ChannelId == toChannel.Id);
            if (game == null)
            {
                await command.RespondAsync("No game found in channel");
                return;
            }
            await command.DeferAsync();
            
            var oldReservations = game.Reservations.Where(reservation => reservation.User == userToEdit.Id);
            foreach (var oldReservation in oldReservations)
            {
                _database.Reservations.Remove(oldReservation);
            }
            await _database.SaveChangesAsync();

            var content = await Utilities.BuildReservationMessage(_countryConfigs, _discord, game);
            
            var reservationsMessage = await Utilities.GetMessageIfCached(toChannel, game.ReservationMessageId);
            if (reservationsMessage != null)
            {
                await ((IUserMessage)reservationsMessage).ModifyAsync(x => { x.Content = content; });
            }
            
            await command.ModifyOriginalResponseAsync(m => m.Content = "Reservation removed");
        }

        private async Task HandleRemoveGameCommand(SocketSlashCommand command)
        {
            var channel = command.Data.Options.FirstOrDefault()?.Value as ISocketMessageChannel;
            var toChannel = channel ?? command.Channel;
            if (toChannel is not SocketGuildChannel guildChannel) return;
            await Utilities.ClearOldGames(toChannel, guildChannel.Guild, _database);
            await command.RespondAsync($"Game removed");
            await _database.SaveChangesAsync();
        }

        private async Task HandleNewGameCommand(SocketSlashCommand command)
        {
            var channel = command.Data.Options.FirstOrDefault()?.Value as ISocketMessageChannel;
            var toChannel = channel ?? command.Channel;
            if (toChannel is not SocketGuildChannel guildChannel) return;
            await command.DeferAsync();
            await Utilities.ClearOldGames(toChannel, guildChannel.Guild, _database);

            var userMessages = await Utilities.BuildReservationMessage(_countryConfigs);
            var replyReservations = await toChannel.SendMessageAsync(userMessages);
            var replyReactionsAllies = await toChannel.SendMessageAsync("Click on reaction to reserve\nAllies:");
            var replyReactionsAxis = await toChannel.SendMessageAsync("Axis:");
            var replyReactionsOther = await toChannel.SendMessageAsync("✋ Will show up\n❌ Cancel reservation:");

            _database.Add(new Game
            {
                ReservationMessageId = replyReservations.Id,
                ReactionsAlliesMessageId = replyReactionsAllies.Id,
                ReactionsAxisMessageId = replyReactionsAxis.Id,
                ReactionsOtherMessageId = replyReactionsOther.Id,
                ChannelId = toChannel.Id,
                GuildId = guildChannel.Guild.Id
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

            if (command.Channel.Id == guildChannel.Id)
            {
                await (await command.GetOriginalResponseAsync()).DeleteAsync();
            }
            else
            {
                await command.ModifyOriginalResponseAsync(m => m.Content = "Game added to another channel");
            }
        }

        private async Task ClientReady()
        {
            var checkCommands = (await _discord.GetGlobalApplicationCommandsAsync()).ToList();
            
            // Let's do our global command
            var newGameCommand = new SlashCommandBuilder()
                .WithName("new_game")
                .WithDescription("Add new game")
                .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel to post the new game, defaults to current channel", isRequired: false);
            
            var removeGameCommand = new SlashCommandBuilder()
                .WithName("remove_game")
                .WithDescription("Remove a game")
                .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel to remove the new game, defaults to current channel", isRequired: false);
            
            var removeReservationCommand = new SlashCommandBuilder()
                .WithName("remove_reservation")
                .WithDescription("Remove users reservation")
                .AddOption("user", ApplicationCommandOptionType.User, "Remove a reservation of a user", isRequired: true)
                .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel to remove the reservation from, defaults to current channel", isRequired: false);


            var permissionsListBuilder = new SlashCommandOptionBuilder()
                .WithName("permission")
                .WithDescription("The rating your willing to give our bot")
                .WithRequired(true)
                .AddChoice("Admin", "admin")
                .AddChoice("Major", "major")
                .AddChoice("Restricted", "restricted")
                .AddChoice("Denied", "deny")
                .WithType(ApplicationCommandOptionType.String);
            var removeRoleCommand = new SlashCommandBuilder()
                .WithName("remove_role")
                .WithDescription("Remove role permission")
                .AddOption("role", ApplicationCommandOptionType.Role, "Role to remove permissions from", true)
                .AddOption(permissionsListBuilder);
            var addRoleCommand = new SlashCommandBuilder()
                .WithName("add_role")
                .WithDescription("Add role permission")
                .AddOption("role", ApplicationCommandOptionType.Role, "Role to add permissions to", true)
                .AddOption(permissionsListBuilder);
            var listRolesCommand = new SlashCommandBuilder()
                .WithName("list_roles")
                .WithDescription("List role permission")
                .AddOption("role", ApplicationCommandOptionType.Role, "Role to list permissions of", true);

            try
            {
                if (!checkCommands.Exists(_ => _.Name == "new_game"))
                    await _discord.CreateGlobalApplicationCommandAsync(newGameCommand.Build());
                if (!checkCommands.Exists(_ => _.Name == "remove_game"))
                    await _discord.CreateGlobalApplicationCommandAsync(removeGameCommand.Build());
                if (!checkCommands.Exists(_ => _.Name == "remove_reservation"))
                    await _discord.CreateGlobalApplicationCommandAsync(removeReservationCommand.Build());
                if (!checkCommands.Exists(_ => _.Name == "remove_role"))
                    await _discord.CreateGlobalApplicationCommandAsync(removeRoleCommand.Build());
                if (!checkCommands.Exists(_ => _.Name == "add_role"))
                    await _discord.CreateGlobalApplicationCommandAsync(addRoleCommand.Build());
                if (!checkCommands.Exists(_ => _.Name == "list_roles"))
                    await _discord.CreateGlobalApplicationCommandAsync(listRolesCommand.Build());
                var remove = checkCommands.Find(_ => _.Name == "newgame");
                if (remove != null) await remove.DeleteAsync();
            }
            catch(HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (rawMessage is not SocketUserMessage { Source: MessageSource.User } message) return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            // Perform prefix check. You may want to replace this with
            // (!message.HasCharPrefix('!', ref argPos))
            // for a more traditional command format like !help.
            if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

            var context = new SocketCommandContext(_discord, message);
            // Perform the execution of the command. In this method,
            // the command service will perform precondition and parsing check
            // then execute the command if one is matched.
            await _commands.ExecuteAsync(context, argPos, _services);
            // Note that normally a result will be returned by this format, but here
            // we will handle the result in CommandExecutedAsync,
        }

        private static async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified)
                return;

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}