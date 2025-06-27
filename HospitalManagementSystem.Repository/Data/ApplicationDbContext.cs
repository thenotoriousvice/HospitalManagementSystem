using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;

namespace HospitalManagementSystem.Repository.Data
{
    /// <summary>
    /// This is the combined ApplicationDbContext, merging configurations from both the Doctor/Appointment
    /// module and the Patient/BookAppointment module. It serves as the single source of truth for the application's database schema.
    /// </summary>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<IdentityUser>(options)
    {

        // --- Combined DbSet Declarations ---
        // These represent the tables in your database. All DbSets from both files are included here.
        public DbSet<Department> Departments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        // Changed from DbSet<Patient> Patients to DbSet<BookAppointment> BookAppointments
        public DbSet<BookedAppointment> BookedAppointments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Patient> Patients { get; set; }

        /// <summary>
        /// This method configures the database model using the Fluent API.
        /// It includes all relationship mappings and constraints from both original DbContext files.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // IMPORTANT: This call is required to configure the ASP.NET Core Identity schema.
            // It must be the first line in this method.
            base.OnModelCreating(modelBuilder);

            // --- Entity Configurations Grouped for Readability ---

            #region BookAppointment Configurations (Replacing Patient Configurations)
            // This section combines all configurations related to the BookAppointment entity.

            // Configure the relationship for BookAppointment and Appointment
            // Assuming Appointment has a foreign key to BookAppointment.
            // This reflects the change from Patient to BookAppointment model for appointments.
            modelBuilder.Entity<BookedAppointment>()
                .HasMany(b => b.Appointments)
                .WithOne(a => a.Patient) // Assuming 'Patient' navigation property in Appointment now refers to BookAppointment
                .HasForeignKey(a => a.PatientId) // PatientId in Appointment now points to BookAppointment's Id
                .OnDelete(DeleteBehavior.SetNull); // If a BookAppointment record is deleted, the Appointment's PatientId is set to null.

            // Property constraints for BookAppointment (adapted from Patient module)
            modelBuilder.Entity<BookedAppointment>()
                .Property(b => b.Name)
                .HasMaxLength(100);

            // Assuming there are no Gender, ContactNumber, Address properties in BookAppointment as per your provided model.
            // If they exist, ensure to add them here similar to Patient configurations.
            #endregion


            #region Doctor Configurations
            // This section contains all configurations related to the Doctor entity.

            // Relationship between Doctor and Department
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany(dept => dept.Doctors)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull); // If a Department is deleted, the Doctor's DepartmentId is set to null.

            // Relationship between Doctor and IdentityUser
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.IdentityUser)
                .WithOne() // One Doctor profile is linked to one IdentityUser.
                .HasForeignKey<Doctor>(d => d.IdentityUserId)
                .IsRequired(false) // A doctor profile can exist without an IdentityUser initially (if needed).
                .OnDelete(DeleteBehavior.Cascade); // If the IdentityUser is deleted, the associated Doctor profile is also deleted.
            #endregion


            #region Appointment Configurations
            // This section contains all configurations related to the Appointment entity.

            // Relationship between Appointment and Doctor
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.SetNull); // If a Doctor is deleted, the Appointment's DoctorId is set to null.

            // Relationship between Appointment and Department
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Department)
                .WithMany() // A department can have many appointments, but we don't need a navigation property on Department.
                .HasForeignKey(a => a.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull); // If a Department is deleted, the Appointment's DepartmentId is set to null.
            #endregion


            // Any future model configurations or data seeding can be added here.
        }
    }
}
