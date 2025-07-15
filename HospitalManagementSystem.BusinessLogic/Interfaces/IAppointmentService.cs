using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic; 
using System; 
using System.Threading.Tasks; 
namespace HospitalManagementSystem.BusinessLogic.Interfaces
{
    public interface IAppointmentService
    {
        
        Task<Appointment> CreateAppointmentAsync(Appointment appointment, bool isExistingPatient);
        Task<IEnumerable<Department>> GetAllDepartmentsAsync();
        Task<IEnumerable<Doctor>> GetDoctorsByDepartmentAsync(int departmentId);
        Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(int doctorId, DateTime date);
        Task<List<Appointment>> GetPatientAppointmentsAsync(string email, string phoneNumber);
        Task<IdentityResult> CancelAppointmentAsync(int appointmentId);
        Task<Patient> GetPatientByUserIdAsync(string userId);

      
        Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(int patientId);

       
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

       
        Task<IEnumerable<Appointment>> GetAllAppointmentsAsync(); //
    }
}