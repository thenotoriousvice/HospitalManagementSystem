using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic; // Added for IEnumerable and List
using System; // Added for DateTime and TimeSpan
using System.Threading.Tasks; // Added for Task

namespace HospitalManagementSystem.BusinessLogic.Interfaces
{
    public interface IAppointmentService
    {
        // Patient-facing methods
        Task<Appointment> CreateAppointmentAsync(Appointment appointment, bool isExistingPatient);
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
        Task<IEnumerable<Doctor>> GetDoctorsByDepartmentAsync(int departmentId);
        Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(int doctorId, DateTime date);
        Task<List<Appointment>> GetPatientAppointmentsAsync(string email, string phoneNumber);
        Task<IdentityResult> CancelAppointmentAsync(int appointmentId);
        Task<Patient> GetPatientByUserIdAsync(string userId);

        // New method for PatientController to get appointments by patient ID
        Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(int patientId);

        // Doctor-facing methods
        Task<IEnumerable<Appointment>> GetAppointmentsForDoctorAsync(int loggedInDoctorId, int? filterDoctorId, int? filterDepartmentId);
        Task<List<Appointment>> GetPendingAppointmentsForDoctorAsync(int doctorId);
        Task<List<Appointment>> GetPendingAppointmentsForDepartmentOnlyAsync(int departmentId);
        Task<IEnumerable<Doctor>> GetAllDoctorsAsync();
        Task<IdentityResult> ApproveAppointmentAsync(int appointmentId, TimeSpan approvedTime);
        Task<IdentityResult> RejectAppointmentAsync(int appointmentId);
        Task<IEnumerable<Appointment>> GetAllAppointmentsForDoctorAsync(int doctorId);

        Task<bool> UpdateAppointmentStatusAsync(int id, AppointmentStatus newStatus);
        Task<bool> CompleteAppointmentAsync(int appointmentId);

        Task<Appointment?> GetAppointmentByBillIdAsync(int billId);

        // New method for Admin Controller to get all appointments
        Task<IEnumerable<Appointment>> GetAllAppointmentsAsync(); //
    }
}