using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Omni.Models;

namespace Omni.Data
{
    public class OmniDbContext : DbContext
    {
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<Music> Music => Set<Music>();
        public DbSet<Video> Videos => Set<Video>();
        public DbSet<Game> Games => Set<Game>();
        public DbSet<Software> Software => Set<Software>();
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

            modelBuilder.Entity<Music>()
                .HasOne(m => m.User).WithMany(u => u.Music)
                .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Video>()
                .HasOne(m => m.User).WithMany(u => u.Videos)
                .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Game>()
                .HasOne(m => m.User).WithMany(u => u.Games)
                .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Software>()
                .HasOne(m => m.User).WithMany(u => u.Software)
                .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);

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

            modelBuilder.Entity<Favourite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favourites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Favourite>()
                .HasIndex(f => new { f.UserId, f.MediaType, f.ItemId })
                .IsUnique();

            modelBuilder.Entity<LaunchHistory>()
                .HasOne(h => h.User)
                .WithMany(u => u.LaunchHistory)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            
        }
    }
}
