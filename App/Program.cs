using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reservator.Models;
using Reservator.Services;

namespace Reservator;

class Program
{
    public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

    public async Task MainAsync()
    {
        BuildConfigurations();
        // You should dispose a service provider created using ASP.NET
        // when you are finished using it, at the end of your app's lifetime.
        // If you use another dependency injection framework, you should inspect
        // its documentation for the best way to do this.
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100
        };
        var client = new DiscordSocketClient(config);
        var services = ConfigureServices(client);
        var db = services.GetRequiredService<GameContext>();
        if (Convert.ToBoolean(Configuration["DEV"]))
        {
            await db.Database.MigrateAsync();
        }
        if (!await db.Database.CanConnectAsync())
        {
            throw new ArgumentException($"No database connection. Check that network settings  are set properly");
        }


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


    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());

        return Task.CompletedTask;
    }

    private void BuildConfigurations()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddEnvironmentVariables();
        Configuration = configurationBuilder.Build();
    }

    private IConfigurationRoot Configuration { get; set; }

    private ServiceProvider ConfigureServices(DiscordSocketClient discordSocketClient)
    {
        var serviceCollection = new ServiceCollection()
            .AddSingleton<CountryConfigService>()
            .AddSingleton(discordSocketClient)
            .AddSingleton<CommandService>()
            .AddSingleton<CommandHandlingService>()
            .AddSingleton<ReactionHandlingService>()
            .AddSingleton<HttpClient>();

        switch (Configuration["DB_TYPE"])
        {
            case "MySql":
                var conn =
                    $"Server={Configuration["DB_HOST"]};Database={Configuration["DB_DATABASE"]};User={Configuration["DB_USER"]};Password={Configuration["DB_PW"]};";
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 22));
                serviceCollection.AddDbContext<GameContext>(options => options.UseMySql(conn, serverVersion));
                break;
            case "Sqlite":
                serviceCollection.AddDbContext<GameContext>(options => options.UseSqlite("Data Source=LocalDatabase.db"));
                break;
            default:
                serviceCollection.AddDbContext<GameContext>(options => options.UseSqlite("Data Source=LocalDatabase.db"));
                Console.WriteLine("Defaulting to Sqlite");
                break;
        }

        return serviceCollection.BuildServiceProvider();
    }
}