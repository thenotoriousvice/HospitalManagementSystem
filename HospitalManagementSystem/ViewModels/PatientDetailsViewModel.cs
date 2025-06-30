// HospitalManagementSystem.ViewModels/PatientDetailsViewModel.cs (Ensure this file exists or create it)

using HospitalManagementSystem.Repository.Models; // Make sure to add this using directive
using System.Collections.Generic;
using System; // For DateTime

namespace HospitalManagementSystem.ViewModels
{
    public class PatientDetailsViewModel
    {
        // Existing properties (from your PatientUpdateViewModel as a base)
        public int PatientId { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public string MedicalHistory { get; set; }
        public string Email { get; set; } // Added email property
        public string PhoneNumber { get; set; } // Added phone number property
        public string IdentityUserName { get; set; } // To display the linked Identity user's username/email

        // NEW PROPERTIES FOR APPOINTMENTS
        public IEnumerable<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public IEnumerable<Appointment> PastAppointments { get; set; } = new List<Appointment>();

        public List<Bill> PatientBills { get; set; } = new List<Bill>();
    }
}