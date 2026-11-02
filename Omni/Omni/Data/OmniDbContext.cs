using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Omni.Models;

namespace Omni.Data
{
    public class OmniDbContext : DbContext
    {
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Playlist> Playlists => Set<Playlist>();
        public DbSet<PlaylistItem> PlaylistItems => Set<PlaylistItem>();
        public DbSet<Favourite> Favourites => Set<Favourite>();
        public DbSet<LaunchHistory> LaunchHistory => Set<LaunchHistory>();

        public static string DatabasePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Omni", "omni.db");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LaunchHistory>().HasKey(h => h.HistoryId);
            modelBuilder.Entity<UserProfile>().HasKey(u => u.UserId);

            modelBuilder.Entity<UserProfile>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Playlist>()
                .HasOne(p => p.User)
                .WithMany(u => u.Playlists)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlaylistItem>()
                .HasOne(pi => pi.Playlist)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlaylistItem>()
                .HasOne(pi => pi.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(pi => pi.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Favourite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favourites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favourite>()
                .HasOne(f => f.PlaylistItem)
                .WithMany()
                .HasForeignKey(f => f.PlaylistItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favourite>()
                .HasIndex(f => new { f.UserId, f.PlaylistItemId })
                .IsUnique();

            modelBuilder.Entity<LaunchHistory>()
                .HasOne(h => h.User)
                .WithMany(u => u.LaunchHistory)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LaunchHistory>()
                .HasOne(h => h.PlaylistItem)
                .WithMany()
                .HasForeignKey(h => h.PlaylistItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Music" },
                new Category { CategoryId = 2, CategoryName = "Video" },
                new Category { CategoryId = 3, CategoryName = "Game" },
                new Category { CategoryId = 4, CategoryName = "Software" });
        }
    }
}
