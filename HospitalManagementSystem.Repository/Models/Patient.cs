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
    [Table("Patient")]
    public class Patient
    {
        // Primary Key for the Patient table.
        [Key]
        public int PatientId { get; set; }

        // Foreign Key to link this patient profile to a Microsoft Identity user account.
        // This will store the UserId (GUID string) from IdentityUser.
        public string IdentityUserId { get; set; }

        // Patient's name, required and limited to 100 characters as per PDF.
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        // Patient's date of birth.
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        // Patient's gender, limited to 10 characters.
        [StringLength(10)]
        public string Gender { get; set; }

        // Patient's contact number, limited to 15 characters.
        [StringLength(15)]
        public string ContactNumber { get; set; }

        // Patient's address, limited to 255 characters.
        [StringLength(255)]
        public string Address { get; set; }

        // Patient's medical history, stored as TEXT.
        public string MedicalHistory { get; set; }

        // Navigation property to the IdentityUser.
        // This allows you to easily access Identity user details from a Patient object.
        public IdentityUser IdentityUser { get; set; }
    }
}
