using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reservator.Models
{
    public class GameContextFactory : IDesignTimeDbContextFactory<GameContext>
    {
        public GameContext CreateDbContext(string[] args)
        {
            var sqlitePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"reservator");
            if (!Directory.Exists(sqlitePath)) Directory.CreateDirectory(sqlitePath);
            var optionsBuilder = new DbContextOptionsBuilder<GameContext>();
            optionsBuilder.UseSqlite($@"Data Source={sqlitePath}\sqlite.db");

            return new GameContext(optionsBuilder.Options);
        }
    }
}