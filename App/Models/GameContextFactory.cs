using System;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Reservator.Models;

public class GameContextFactory : IDesignTimeDbContextFactory<GameContext>
{
    public GameContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
        
        switch (configuration["DB_TYPE"])
        {
            case "MySql":
                var conn =
                    $"Server={configuration["DB_HOST"]};Database={configuration["DB_DATABASE"]};User={configuration["DB_USER"]};Password={configuration["DB_PW"]};";
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 22));
                optionsBuilder.UseMySql(conn, serverVersion);
                break;
            case "Sqlite":
                optionsBuilder.UseSqlite("Data Source=LocalDatabase.db");
                break;
            default:
                optionsBuilder.UseSqlite("Data Source=LocalDatabase.db");
                Console.WriteLine("Defaulting to Sqlite");
                break;
        }

  
        return new GameContext(optionsBuilder.Options);
    }
}