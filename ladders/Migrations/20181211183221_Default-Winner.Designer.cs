﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ladders.Models;

namespace ladders.Migrations
{
    [DbContext(typeof(LaddersContext))]
    [Migration("20181211183221_Default-Winner")]
    partial class DefaultWinner
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("ladders.Models.Booking", b =>
                {
                    b.Property<int>("bookingId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("bookingDateTime");

                    b.Property<int?>("facilityId");

                    b.Property<string>("userId");

                    b.HasKey("bookingId");

                    b.HasIndex("facilityId");

                    b.ToTable("Booking");
                });

            modelBuilder.Entity("ladders.Models.Challenge", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("BookingId");

                    b.Property<DateTime>("ChallengedTime");

                    b.Property<int?>("ChallengeeId");

                    b.Property<int?>("ChallengerId");

                    b.Property<DateTime>("Created");

                    b.Property<int?>("LadderId");

                    b.Property<int?>("RankingId");

                    b.Property<bool>("Resolved");

                    b.Property<int>("Result");

                    b.HasKey("Id");

                    b.HasIndex("BookingId");

                    b.HasIndex("ChallengeeId");

                    b.HasIndex("ChallengerId");

                    b.HasIndex("LadderId");

                    b.HasIndex("RankingId");

                    b.ToTable("Challenge");
                });

            modelBuilder.Entity("ladders.Models.Facility", b =>
                {
                    b.Property<int>("facilityId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("facilityName");

                    b.Property<bool>("isBlock");

                    b.Property<int>("sportId");

                    b.Property<int>("venueId");

                    b.HasKey("facilityId");

                    b.HasIndex("sportId");

                    b.HasIndex("venueId");

                    b.ToTable("Facility");
                });

            modelBuilder.Entity("ladders.Models.LadderModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("LadderModel");
                });

            modelBuilder.Entity("ladders.Models.ProfileModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("ApprovalLadderId");

                    b.Property<string>("Availability")
                        .IsRequired();

                    b.Property<int?>("CurrentRankingId");

                    b.Property<string>("Name");

                    b.Property<string>("PreferredLocation")
                        .IsRequired();

                    b.Property<bool>("Suspended");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("ApprovalLadderId");

                    b.HasIndex("CurrentRankingId")
                        .IsUnique();

                    b.ToTable("ProfileModel");
                });

            modelBuilder.Entity("ladders.Models.Ranking", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Draws");

                    b.Property<int?>("LadderModelId")
                        .IsRequired();

                    b.Property<int>("Losses");

                    b.Property<int>("Wins");

                    b.HasKey("Id");

                    b.HasIndex("LadderModelId");

                    b.ToTable("Ranking");
                });

            modelBuilder.Entity("ladders.Models.Sport", b =>
                {
                    b.Property<int>("sportId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("sportName");

                    b.HasKey("sportId");

                    b.ToTable("Sport");
                });

            modelBuilder.Entity("ladders.Models.Venue", b =>
                {
                    b.Property<int>("venueId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("venueName");

                    b.HasKey("venueId");

                    b.ToTable("Venue");
                });

            modelBuilder.Entity("ladders.Models.Booking", b =>
                {
                    b.HasOne("ladders.Models.Facility", "facility")
                        .WithMany()
                        .HasForeignKey("facilityId");
                });

            modelBuilder.Entity("ladders.Models.Challenge", b =>
                {
                    b.HasOne("ladders.Models.Booking", "Booking")
                        .WithMany()
                        .HasForeignKey("BookingId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ladders.Models.ProfileModel", "Challengee")
                        .WithMany()
                        .HasForeignKey("ChallengeeId");

                    b.HasOne("ladders.Models.ProfileModel", "Challenger")
                        .WithMany()
                        .HasForeignKey("ChallengerId");

                    b.HasOne("ladders.Models.LadderModel", "Ladder")
                        .WithMany()
                        .HasForeignKey("LadderId");

                    b.HasOne("ladders.Models.Ranking")
                        .WithMany("Challenges")
                        .HasForeignKey("RankingId");
                });

            modelBuilder.Entity("ladders.Models.Facility", b =>
                {
                    b.HasOne("ladders.Models.Sport", "sport")
                        .WithMany()
                        .HasForeignKey("sportId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("ladders.Models.Venue", "venue")
                        .WithMany()
                        .HasForeignKey("venueId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ladders.Models.ProfileModel", b =>
                {
                    b.HasOne("ladders.Models.LadderModel", "ApprovalLadder")
                        .WithMany("ApprovalUsersList")
                        .HasForeignKey("ApprovalLadderId");

                    b.HasOne("ladders.Models.Ranking", "CurrentRanking")
                        .WithOne("User")
                        .HasForeignKey("ladders.Models.ProfileModel", "CurrentRankingId");
                });

            modelBuilder.Entity("ladders.Models.Ranking", b =>
                {
                    b.HasOne("ladders.Models.LadderModel", "LadderModel")
                        .WithMany("CurrentRankings")
                        .HasForeignKey("LadderModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
