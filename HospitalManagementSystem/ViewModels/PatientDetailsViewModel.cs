using HospitalManagementSystem.Repository.Models; 
using System.Collections.Generic;
using System; 

namespace HospitalManagementSystem.ViewModels
{
    public class PatientDetailsViewModel
    {
       
        public int PatientId { get; set; }
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public string MedicalHistory { get; set; }
        public string Email { get; set; } 
        public string PhoneNumber { get; set; } 
        public string IdentityUserName { get; set; } 
       
        public IEnumerable<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public IEnumerable<Appointment> PastAppointments { get; set; } = new List<Appointment>();

        public List<Bill> PatientBills { get; set; } = new List<Bill>();
    }
}