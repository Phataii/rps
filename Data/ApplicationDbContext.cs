using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using rps.Models;

namespace rps.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<DepartmentBatch> DepartmentBatches { get; set; }
    public DbSet<FacultyBatch> FacultyBatches { get; set; }
    public DbSet<TranscriptApplication> TranscriptApplications { get; set; }
    public DbSet<Result> Results { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Roles> Roles { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Remark> Remarks { get; set; }
    public DbSet<LevelAdviser> LevelAdvisers { get; set; }
}
