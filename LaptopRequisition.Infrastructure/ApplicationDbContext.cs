using LaptopRequisition.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using LaptopRequisition.Domain.Enums;

namespace LaptopRequisition.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Laptop> Laptops { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.Property(e => e.StaffId).HasMaxLength(255);
                entity.HasIndex(e => e.StaffId).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentId);

                entity.HasOne(e => e.Role)
                      .WithMany(r => r.Employees)
                      .HasForeignKey(e => e.RoleId);

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasMany(d => d.Employees)
                      .WithOne(e => e.Department)
                      .HasForeignKey(e => e.DepartmentId);

                entity.Property(d => d.Name).HasMaxLength(255);
                entity.HasIndex(d => d.Name).IsUnique();

                entity.HasData(
                    new Department
                    {
                        Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        Name = "IT",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                );
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(r => r.Name).HasMaxLength(50);
                entity.HasIndex(r => r.Name).IsUnique();

                entity.HasData(
                    new Role
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Name = "Admin",
                        Description = "Administrator with full access",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Role // Seed Employee Role
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Name = "Employee",
                        Description = "Standard employee with limited access",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                );
            });

            modelBuilder.Entity<Laptop>(entity =>
            {
                entity.Property(l => l.SerialNumber).HasMaxLength(255);
                entity.HasIndex(l => l.SerialNumber).IsUnique();
                entity.Property(l => l.Status)
                      .HasConversion<string>();
            });

            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasIndex(r => r.EmployeeId);
                entity.HasIndex(r => r.Status);
                entity.Property(r => r.Status)
                      .HasConversion<string>();
                entity.HasOne(r => r.Employee)
                      .WithMany(e => e.Requests)
                      .HasForeignKey(r => r.EmployeeId);

                entity.HasOne(r => r.Laptop)
                      .WithMany(l => l.Requests)
                      .HasForeignKey(r => r.LaptopId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReturnRequest>(entity =>
            {
                entity.HasOne(rr => rr.Employee)
                      .WithMany(e => e.ReturnRequests)
                      .HasForeignKey(rr => rr.EmployeeId);
                entity.HasOne(rr => rr.Laptop)
                      .WithMany(l => l.ReturnRequests)
                      .HasForeignKey(rr => rr.LaptopId);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.Employee)
                      .WithMany(e => e.Notifications)
                      .HasForeignKey(n => n.EmployeeId);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.Property(prt => prt.Token).HasMaxLength(255);
                entity.HasIndex(prt => prt.Token).IsUnique();
                entity.HasOne(prt => prt.Employee)
                      .WithMany(e => e.PasswordResetTokens)
                      .HasForeignKey(prt => prt.EmployeeId);
            });
        }

        public override int SaveChanges()
        {
            AddAuditInfo();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddAuditInfo();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void AddAuditInfo()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => (e.Entity is Request || e.Entity is Employee || e.Entity is Department || e.Entity is Laptop || e.Entity is ReturnRequest || e.Entity is Role) && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted));

            foreach (var entry in entries)
            {
                if (entry.Entity is Request request)
                {
                    if (entry.State == EntityState.Added)
                    {
                        request.CreatedAt = DateTime.UtcNow;
                        request.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        request.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is Employee employee)
                {
                    if (entry.State == EntityState.Added)
                    {
                        employee.CreatedAt = DateTime.UtcNow;
                        employee.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        employee.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        entry.State = EntityState.Modified;
                        employee.IsDeleted = true;
                        employee.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is Department department)
                {
                    if (entry.State == EntityState.Added)
                    {
                        department.CreatedAt = DateTime.UtcNow;
                        department.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        department.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is Laptop laptop)
                {
                    if (entry.State == EntityState.Added)
                    {
                        laptop.CreatedAt = DateTime.UtcNow;
                        laptop.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        laptop.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is ReturnRequest returnRequest)
                {
                    if (entry.State == EntityState.Added)
                    {
                        returnRequest.CreatedAt = DateTime.UtcNow;
                        returnRequest.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        returnRequest.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is Role role)
                {
                    if (entry.State == EntityState.Added)
                    {
                        role.CreatedAt = DateTime.UtcNow;
                        role.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        role.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}