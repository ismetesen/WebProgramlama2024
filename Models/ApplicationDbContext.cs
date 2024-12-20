using Microsoft.EntityFrameworkCore;

namespace BarberApplication.Models
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<EmployeePerformance> EmployeePerformances { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Fluent API kullanarak ilişkiler
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User-Email için benzersiz kısıtlama
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Appointment-Employee için Foreign Key ilişkisinin tanımlanması
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Appointments)
                .HasForeignKey(a => a.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict); // Silme davranışı

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ServiceID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeePerformance>()
                .HasOne(ep => ep.Employee)
                .WithMany(e => e.Performances)
                .HasForeignKey(ep => ep.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

