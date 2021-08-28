using System;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Reservator.Models
{
    public class GameContextFactory : IDesignTimeDbContextFactory<GameContext>
    {
        public GameContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
            
            var config = new StringBuilder("Server=ENVHOST;Database=ENVDB;User=ENVUSER;Password=ENVPW;");
            var conn = config.Replace("ENVHOST", configuration["DB_HOST"])
                .Replace("ENVDB", configuration["DB_DATABASE"])
                .Replace("ENVUSER", configuration["DB_USER"])
                .Replace("ENVPW", configuration["DB_PW"])
                .ToString();
            optionsBuilder.UseSqlite(configuration["ConnectionStrings:ConnectionMssql"]);

            return new GameContext(optionsBuilder.Options);
        }
    }
}