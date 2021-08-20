﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Reservator.Models;

namespace Reservator.Migrations
{
    [DbContext(typeof(GameContext))]
    [Migration("20210817203304_ReactionOther")]
    partial class ReactionOther
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.9");

            modelBuilder.Entity("Reservator.Models.Game", b =>
                {
                    b.Property<int>("GameId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ReactionsAlliesMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ReactionsAxisMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ReactionsOtherMessageId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ReservationMessageId")
                        .HasColumnType("INTEGER");

                    b.HasKey("GameId");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("Reservator.Models.Reservation", b =>
                {
                    b.Property<int>("ReservationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Country")
                        .HasColumnType("TEXT");

                    b.Property<int?>("GameId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("User")
                        .HasColumnType("INTEGER");

                    b.HasKey("ReservationId");

                    b.HasIndex("GameId");

                    b.ToTable("Reservations");
                });

            modelBuilder.Entity("Reservator.Models.Reservation", b =>
                {
                    b.HasOne("Reservator.Models.Game", "Game")
                        .WithMany("Reservations")
                        .HasForeignKey("GameId");

                    b.Navigation("Game");
                });

            modelBuilder.Entity("Reservator.Models.Game", b =>
                {
                    b.Navigation("Reservations");
                });
#pragma warning restore 612, 618
        }
    }
}
