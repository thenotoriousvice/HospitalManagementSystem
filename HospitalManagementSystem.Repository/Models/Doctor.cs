using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagementSystem.Repository.Models
{
    public class Doctor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // Keeping 'Id' for consistency with existing appointments

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } // From DoctorManagement

        [Phone]
        public string PhoneNumber { get; set; } // From DoctorManagement

        public string Qualification { get; set; } // From DoctorManagement

        public int ExperienceYears { get; set; } // From DoctorManagement

        // Doctor's daily working hours (replaces string Availability)
        [DataType(DataType.Time)]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan? WorkingHoursStart { get; set; }

        [DataType(DataType.Time)]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan? WorkingHoursEnd { get; set; }

        // Foreign key to IdentityUser
        [ForeignKey("IdentityUser")]
        public string? IdentityUserId { get; set; } // Nullable, as initial seeding might not have a user
        public IdentityUser? IdentityUser { get; set; } // Navigation property

        // Existing relationship to Department
        public int? DepartmentId { get; set; } // Made nullable since a doctor might register before department assignment
        public Department? Department { get; set; }

        // Existing relationship to Appointments
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
