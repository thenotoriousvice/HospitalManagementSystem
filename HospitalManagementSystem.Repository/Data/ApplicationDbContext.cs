using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;

namespace HospitalManagementSystem.Repository.Data
{
    
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<IdentityUser>(options)
    {

       
        public DbSet<Department> Departments { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        
        public DbSet<BookedAppointment> BookedAppointments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Patient> Patients { get; set; }

        public DbSet<Bill> Bills { get; set; }

      
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
            base.OnModelCreating(modelBuilder);

            

            #region BookAppointment Configurations (Replacing Patient Configurations)
          
            modelBuilder.Entity<Patient>()
                .HasMany(b => b.Appointments)
                .WithOne(a => a.Patient) 
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.SetNull); 

           
            modelBuilder.Entity<BookedAppointment>()
                .Property(b => b.Name)
                .HasMaxLength(100);

          
            #endregion


            #region Doctor Configurations
           

           
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


            // Relationship between Appointment and Patient
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)          
                .WithMany(p => p.Appointments)  
                .HasForeignKey(a => a.PatientId) 
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Bill>()
               .HasOne(b => b.Appointment)      
               .WithOne(a => a.Bill)             
               .HasForeignKey<Bill>(b => b.AppointmentId) 
               .IsRequired(false)               
               .OnDelete(DeleteBehavior.SetNull);
            #endregion


           
        }
    }
}
