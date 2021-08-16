using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Reservator.Preconditions;

namespace Reservator.Modules
{
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("newgame")]
        public async Task NewGame()
        {
            var builder = new ComponentBuilder()
                .WithSelectMenu(new SelectMenuBuilder()
                    .WithCustomId("id_2")
                    .WithPlaceholder("This is a placeholder")
                    .AddOption(
                        label: "Option",
                        value: "value1",
                        description: "Evan pog champ",
                        emote: Emote.Parse("<:evanpog:810017136814194698>")
                    )
                    .AddOption("Option B", "value2", "Option B is poggers")
                );
  
            await ReplyAsync("Test selection!", component: builder.Build());
        }
    }
}