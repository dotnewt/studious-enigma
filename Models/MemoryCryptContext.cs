using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace studious_enigma.Models;

public partial class MemoryCryptContext : DbContext
{
    public MemoryCryptContext()
    {
    }

    public MemoryCryptContext(DbContextOptions<MemoryCryptContext> options)
        : base(options)
    {
    }

    public DbSet<Article> Articles { get; set; } = null;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
