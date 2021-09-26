using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reservator.Models;

namespace Migrator
{
    class Program
    {
        public static void Main(string[] args)
            => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync() {
        var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddEnvironmentVariables();
            var configuration = configurationBuilder.Build();
            
            var config = new StringBuilder("Server=ENVHOST;Database=ENVDB;User=ENVUSER;Password=ENVPW;");
            var conn = config.Replace("ENVHOST", configuration["DB_HOST"])
                    .Replace("ENVDB", configuration["DB_DATABASE"])
                    .Replace("ENVUSER", configuration["DB_USER"])
                    .Replace("ENVPW", configuration["DB_PW"])
                    .ToString();
            var services = new ServiceCollection().AddDbContext<GameContext>(options => options.UseSqlServer(conn)).BuildServiceProvider();
            var db = services.GetRequiredService<GameContext>();
            await db.Database.MigrateAsync();
        }
    }
}