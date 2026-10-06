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
        public DbSet<MediaItem> MediaItems => Set<MediaItem>();
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
            modelBuilder.Entity<MediaItem>().HasKey(m => m.MediaId);

            modelBuilder.Entity<UserProfile>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<MediaItem>()
                .HasOne(m => m.User)
                .WithMany(u => u.MediaItems)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MediaItem>()
                .HasOne(m => m.Category)
                .WithMany(c => c.MediaItems)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

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
                .HasOne(pi => pi.MediaItem)
                .WithMany(m => m.PlaylistItems)
                .HasForeignKey(pi => pi.MediaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favourite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favourites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favourite>()
                .HasOne(f => f.MediaItem)
                .WithMany(m => m.Favourites)
                .HasForeignKey(f => f.MediaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favourite>()
                .HasIndex(f => new { f.UserId, f.MediaId })
                .IsUnique();

            modelBuilder.Entity<LaunchHistory>()
                .HasOne(h => h.User)
                .WithMany(u => u.LaunchHistory)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LaunchHistory>()
                .HasOne(h => h.MediaItem)
                .WithMany(m => m.LaunchHistory)
                .HasForeignKey(h => h.MediaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Music" },
                new Category { CategoryId = 2, CategoryName = "Video" },
                new Category { CategoryId = 3, CategoryName = "Game" },
                new Category { CategoryId = 4, CategoryName = "Software" });
        }
    }
}
