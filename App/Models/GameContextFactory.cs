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
            
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 22));
            var config = new StringBuilder("server=ENVHOST;database=ENVDB;user=ENVUSER;password=ENVPW;");
            var conn = config.Replace("ENVHOST", configuration["DB_HOST"])
                .Replace("ENVDB", configuration["DB_DATABASE"])
                .Replace("ENVUSER", configuration["DB_USER"])
                .Replace("ENVPW", configuration["DB_PW"])
                .ToString();
            
            optionsBuilder.UseMySql(conn, serverVersion);

  
            return new GameContext(optionsBuilder.Options);
        }
    }
}