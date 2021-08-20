﻿using System;
using Microsoft.EntityFrameworkCore;

namespace Reservator.Models
{
    public class GameContext : DbContext
    {
        public DbSet<Game> Games { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<GuildRoles> GuildRoles { get; set; }

        public GameContext(DbContextOptions<GameContext> options)
            : base(options)
        {
        }
        
        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
            }
        }
    
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuildRoles>().HasKey(vf=> new {vf.GuildId, vf.RoleId, vf.Type});
        }
    }
}