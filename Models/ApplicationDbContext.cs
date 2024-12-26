using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;


namespace BarberApplication.Models
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<EmployeePerformance> EmployeePerformances { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=BarberShop;Trusted_Connection=True;");
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Employee>().HasMany(e => e.Appointments).WithOne(a => a.Employee).HasForeignKey(a => a.EmployeeID).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Employee>().HasMany(e => e.Performances).WithOne(p => p.Employee).HasForeignKey(p => p.EmployeeID).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Service>().HasMany(s => s.Appointments).WithOne(a => a.Service).HasForeignKey(a => a.ServiceID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<User>().HasMany(u => u.Appointments).WithOne(a => a.User).HasForeignKey(a => a.UserID).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EmployeePerformance>()
            .HasKey(e => e.PerformanceID); // PerformanceID'yi birincil anahtar olarak ayarlıyoruz
        }
    }
}

