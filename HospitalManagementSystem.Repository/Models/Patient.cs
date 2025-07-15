using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagementSystem.Repository.Models
{
    [Table("Patient")]
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        public string IdentityUserId { get; set; }
        public IdentityUser IdentityUser { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [StringLength(10)]
        public string Gender { get; set; }

        [StringLength(15)]
        public string ContactNumber { get; set; }

        [Required] 
        [EmailAddress]
        [StringLength(255)] 
        public string Email { get; set; } 

        [Required]
        [Phone]
        [StringLength(20)] 
        public string PhoneNumber { get; set; } 

        [StringLength(255)]
        public string Address { get; set; }

        public string MedicalHistory { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}