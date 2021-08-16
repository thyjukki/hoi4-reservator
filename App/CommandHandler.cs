using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Reservator
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }
        
        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;
            _client.InteractionCreated += Client_InteractionCreated;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), 
                                            services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) || 
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commands.ExecuteAsync(
                context: context, 
                argPos: argPos,
                services: null);
        }
        
        private async Task MyMessageComponentHandler(SocketMessageComponent interaction)
        {
            // Get the custom ID 
            var customId = interaction.Data.CustomId;
            // Get the user
            var user = (SocketGuildUser) interaction.User;
            // Get the guild
            var guild = user.Guild;
    
            // Respond with the update message. This edits the message which this component resides.
            await interaction.UpdateAsync(msgProps => msgProps.Content = $"Clicked {interaction.Data.CustomId}!");
    
            // Also you can followup with a additional messages
            await interaction.FollowupAsync($"Clicked {interaction.Data.CustomId}!", ephemeral: true);
    
            // If you are using selection dropdowns, you can get the selected label and values using these
            var selectedLabel = ((SelectMenu) interaction.Message.Components.First().Components.First()).Options.FirstOrDefault(x => x.Value == interaction.Data.Values.FirstOrDefault())?.Label;
            var selectedValue = interaction.Data.Values.First();
        }
        
        private async Task Client_InteractionCreated(SocketInteraction interaction)
        {
            // Checking the type of this interaction
            switch (interaction)
            {
      
                // Button clicks/selection dropdowns
                case SocketMessageComponent componentInteraction:
                    await MyMessageComponentHandler(componentInteraction);
                    break;
      
                // Unused or Unknown/Unsupported
                default:
                    break;
            }
        }
    }
}