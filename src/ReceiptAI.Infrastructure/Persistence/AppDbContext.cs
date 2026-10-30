using System;
using Microsoft.EntityFrameworkCore;

namespace ReceiptAI.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    public DbSet<DocumentChunkRecord> DocumentChunks =>
        Set<DocumentChunkRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable PostgreSQL pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Automatically discover EF Core configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
