namespace Model.Configuration;

public class ModelDbContext : DbContext {
    public ModelDbContext(DbContextOptions<ModelDbContext> options) : base(options) {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<RoleClaim> RoleClaims { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder) {

        builder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        builder.Entity<Role>()
            .HasIndex(r => r.Identifier)
            .IsUnique();

        builder.Entity<RoleClaim>()
            .HasKey(rc => new { rc.UserId, rc.RoleId });

        builder.Entity<RoleClaim>()
            .HasOne(rc => rc.Role)
            .WithMany(r => r.RoleClaims)
            .HasForeignKey(rc => rc.RoleId);

        builder.Entity<RoleClaim>()
            .HasOne(rc => rc.User)
            .WithMany(u => u.RoleClaims)
            .HasForeignKey(rc => rc.UserId);
        
        builder.Entity<Session>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId);
        
        builder.Entity<Session>()
            .HasIndex(s => s.Token)
            .IsUnique();
        
        // SEEDING
        builder.Entity<Role>()
            .HasData(new Role { Id = 1, Identifier = "Admin", Description = "Administrator" });
    }
}