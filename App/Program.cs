using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reservator.Models;
using Reservator.Services;

namespace Reservator
{
    class Program
    {
        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            // You should dispose a service provider created using ASP.NET
            // when you are finished using it, at the end of your app's lifetime.
            // If you use another dependency injection framework, you should inspect
            // its documentation for the best way to do this.
            var config = new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 100
            };
            var client = new DiscordSocketClient(config);
            var services = ConfigureServices(client);
            //using (var services = ConfigureServices())
            {

                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
                await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("BOT_TOKEN"));
                await client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                services.GetRequiredService<ReactionHandlingService>();

                await Task.Delay(Timeout.Infinite);
            }
        }
        

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }
        
        private ServiceProvider ConfigureServices(DiscordSocketClient discordSocketClient)
        {
            var sqlitePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"reservator");
            if (!Directory.Exists(sqlitePath)) Directory.CreateDirectory(sqlitePath);
            return new ServiceCollection()
                .AddDbContext<GameContext>(options => options.UseSqlite($@"Data Source={sqlitePath}\sqlite.db"))
                .AddSingleton<CountryConfigService>()
                .AddSingleton(discordSocketClient)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<ReactionHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<PictureService>()
                .BuildServiceProvider();
        }
    }
}
