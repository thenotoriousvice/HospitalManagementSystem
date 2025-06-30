using HospitalManagementSystem.Repository.Models;

namespace HospitalManagementSystem.ViewModels
{
    public class DoctorAppointmentViewModel
    {
        public Appointment Appointment { get; set; }
        public Bill? Bill { get; set; } // Nullable because not all appointments will have a bill
    }
}