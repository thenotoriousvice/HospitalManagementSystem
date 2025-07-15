using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Data;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HospitalManagementSystem.BusinessLogic.Implementation
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AppointmentService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<Appointment> CreateAppointmentAsync(Appointment appointment, bool isExistingPatient)
        {
            try
            {
                appointment.RequestedAt = DateTime.Now;
                appointment.Status = AppointmentStatus.Pending;

                // patient details based on whether it's an existing patient
                if (isExistingPatient)
                {
                    // If it's an existing patient and PatientId is not already set, try to find them
                    if (!appointment.PatientId.HasValue)
                    {
                        var existingPatient = await _context.Patients
                            .FirstOrDefaultAsync(p => p.Email == appointment.PatientEmail ||
                                               p.PhoneNumber == appointment.PatientPhoneNumber);

                        if (existingPatient != null)
                        {
                            appointment.PatientId = existingPatient.PatientId;
                            appointment.Patient = existingPatient;
                        }
                        else
                        {
                            // If patient not found, create anew patient
                            var newPatient = new Patient
                            {
                                Name = appointment.PatientName,
                                Email = appointment.PatientEmail,
                                PhoneNumber = appointment.PatientPhoneNumber
                            };

                            _context.Patients.Add(newPatient);
                            await _context.SaveChangesAsync();
                            appointment.PatientId = newPatient.PatientId;
                            appointment.Patient = newPatient;
                        }
                    }
                }
                else
                {
                    // Create new patient
                    var newPatient = new Patient
                    {
                        Name = appointment.PatientName,
                        Email = appointment.PatientEmail,
                        PhoneNumber = appointment.PatientPhoneNumber
                    };

                    _context.Patients.Add(newPatient);
                    await _context.SaveChangesAsync();
                    appointment.PatientId = newPatient.PatientId;
                    appointment.Patient = newPatient;
                }

                // Automatic Doctor Allotment for Department-only bookings
                if (appointment.DoctorId == null && appointment.DepartmentId.HasValue && appointment.AppointmentTime.HasValue)
                {
                    var doctorsInDepartment = await _context.Doctors
                        .Where(d => d.DepartmentId == appointment.DepartmentId.Value)
                        .ToListAsync();

                    Doctor allottedDoctor = null;
                    foreach (var doctor in doctorsInDepartment)
                    {
                        if (doctor.WorkingHoursStart.HasValue && doctor.WorkingHoursEnd.HasValue &&
                            appointment.AppointmentTime.Value >= doctor.WorkingHoursStart.Value &&
                            appointment.AppointmentTime.Value < doctor.WorkingHoursEnd.Value)
                        {
                            bool isSlotBooked = await _context.Appointments
                                .AnyAsync(a => a.DoctorId == doctor.Id &&
                                       a.AppointmentDate == appointment.AppointmentDate &&
                                       a.AppointmentTime == appointment.AppointmentTime &&
                                       a.Status == AppointmentStatus.Approved);

                            if (!isSlotBooked)
                            {
                                allottedDoctor = doctor;
                                break;
                            }
                        }
                    }

                    if (allottedDoctor != null)
                    {
                        appointment.DoctorId = allottedDoctor.Id;
                        appointment.Doctor = allottedDoctor;
                    }
                    else
                    {
                        return null; // No doctor available for the requested time slot
                    }
                }
                else if (appointment.DoctorId.HasValue && appointment.AppointmentTime.HasValue)
                {
                    var selectedDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == appointment.DoctorId.Value);

                    if (selectedDoctor == null ||
                        !selectedDoctor.WorkingHoursStart.HasValue || !selectedDoctor.WorkingHoursEnd.HasValue ||
                        appointment.AppointmentTime.Value < selectedDoctor.WorkingHoursStart.Value ||
                        appointment.AppointmentTime.Value >= selectedDoctor.WorkingHoursEnd.Value)
                    {
                        return null; // invalid time slot for this doctor's working hours
                    }

                    bool isSlotBooked = await _context.Appointments
                        .AnyAsync(a => a.DoctorId == appointment.DoctorId.Value &&
                                       a.AppointmentDate == appointment.AppointmentDate &&
                                       a.AppointmentTime == appointment.AppointmentTime &&
                                       a.Status == AppointmentStatus.Approved);

                    if (isSlotBooked)
                    {
                        return null; // Slot already booked for this specific doctor
                    }
                }

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return appointment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating appointment: {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetDoctorsByDepartmentAsync(int departmentId)
        {
            return await _context.Doctors
                                 .Where(d => d.DepartmentId == departmentId)
                                 .ToListAsync();
        }

        public async Task<List<TimeSpan>> GetAvailableTimeSlotsAsync(int doctorId, DateTime date)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null || !doctor.WorkingHoursStart.HasValue || !doctor.WorkingHoursEnd.HasValue)
            {
                return new List<TimeSpan>();
            }

            var bookedAppointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                            a.AppointmentDate == date &&
                            (a.Status == AppointmentStatus.Approved || a.Status == AppointmentStatus.Pending) &&
                            a.AppointmentTime.HasValue)
                .Select(a => a.AppointmentTime.Value)
                .ToListAsync();

            List<TimeSpan> availableSlots = new List<TimeSpan>();
            TimeSpan currentTime = doctor.WorkingHoursStart.Value;
            TimeSpan appointmentDuration = TimeSpan.FromMinutes(30);

            while (currentTime < doctor.WorkingHoursEnd.Value)
            {
                if (!bookedAppointments.Contains(currentTime))
                {
                    availableSlots.Add(currentTime);
                }
                currentTime = currentTime.Add(appointmentDuration);
            }
            return availableSlots;
        }

        public async Task<List<Appointment>> GetPatientAppointmentsAsync(string email, string phoneNumber)
        {
           
            return await _context.Appointments
                .Include(a => a.Patient) 
                .Include(a => a.Doctor)
                .Include(a => a.Department)
                .Where(a => a.PatientEmail == email && a.PatientPhoneNumber == phoneNumber)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.RequestedAt)
                .ToListAsync();
        }

        
        public async Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Department)
                .Include(a => a.Bill)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<Patient> GetPatientByUserIdAsync(string userId)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.IdentityUserId == userId);
        }

        public async Task<IdentityResult> CancelAppointmentAsync(int appointmentId)
        {
            var appointmentToCancel = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointmentToCancel == null || appointmentToCancel.Status == AppointmentStatus.Cancelled)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Appointment not found or already cancelled." });
            }

            appointmentToCancel.Status = AppointmentStatus.Cancelled;
            appointmentToCancel.ApprovedRejectedAt = DateTime.Now;
            _context.Update(appointmentToCancel);
            await _context.SaveChangesAsync();

            string formattedAppointmentTime = appointmentToCancel.AppointmentTime.HasValue
                ? new DateTime().Add(appointmentToCancel.AppointmentTime.Value).ToString("hh:mm tt")
                : "an unassigned time"; 

            await SendAppointmentEmailAsync(
                appointmentToCancel.PatientEmail,
                $"Your Appointment has been Cancelled - {appointmentToCancel.AppointmentDate:yyyy-MM-dd}",
                $"Dear {appointmentToCancel.PatientName},\n\nYour appointment with Dr. {appointmentToCancel.Doctor?.FullName} " +
                $"({appointmentToCancel.Department?.Name} Department) on {appointmentToCancel.AppointmentDate:dddd, MMMM dd,yyyy} " +
                $"at {formattedAppointmentTime} has been CANCELLED.\n\n" +
                $"Please contact the hospital for further assistance.\n\nSincerely,\nHospital Management Team"
            );

            return IdentityResult.Success;
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsForDoctorAsync(int loggedInDoctorId, int? filterDoctorId, int? filterDepartmentId)
        {
            IQueryable<Appointment> query = _context.Appointments
                .Include(a => a.Patient) 
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.IdentityUser)
                .Include(a => a.Department);

            if (filterDoctorId.HasValue && filterDoctorId.Value != 0)
            {
                query = query.Where(a => a.DoctorId == filterDoctorId.Value);
            }
            else
            {
                query = query.Where(a => a.DoctorId == loggedInDoctorId);
            }

            if (filterDepartmentId.HasValue && filterDepartmentId.Value != 0)
            {
                query = query.Where(a => a.DepartmentId == filterDepartmentId.Value);
            }

            query = query.Where(a => a.Status == AppointmentStatus.Pending)
                         .OrderBy(a => a.AppointmentDate)
                         .ThenBy(a => a.AppointmentTime);

            return await query.ToListAsync();
        }

        public async Task<List<Appointment>> GetPendingAppointmentsForDoctorAsync(int doctorId)
        {
            return await _context.Appointments
                .Include(a => a.Patient) 
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.IdentityUser)
                .Include(a => a.Department)
                .Where(a => a.DoctorId == doctorId && a.Status == AppointmentStatus.Pending)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetPendingAppointmentsForDepartmentOnlyAsync(int departmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient) 
                .Include(a => a.Doctor)
                .Include(a => a.Department)
                .Where(a => a.DepartmentId == departmentId && a.DoctorId == null && a.Status == AppointmentStatus.Pending)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
        {
            return await _context.Doctors.Include(d => d.Department).ToListAsync();
        }

        public async Task<IdentityResult> ApproveAppointmentAsync(int appointmentId, TimeSpan approvedTime)
        {
            var appointmentToApprove = await _context.Appointments
                .Include(a => a.Patient) 
                .Include(a => a.Doctor)
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointmentToApprove == null || appointmentToApprove.Status != AppointmentStatus.Pending)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Appointment not found or not pending." });
            }

            // Checking if the chosen time slot is already booked for this doctor
            bool isSlotBookedForApproval = await _context.Appointments
                .AnyAsync(a => a.DoctorId == appointmentToApprove.DoctorId &&
                               a.AppointmentDate == appointmentToApprove.AppointmentDate &&
                               a.AppointmentTime == approvedTime &&
                               a.Status == AppointmentStatus.Approved);

            if (isSlotBookedForApproval)
            {
                Console.WriteLine($"Attempt to approve already booked slot for DoctorId {appointmentToApprove.DoctorId} at {appointmentToApprove.AppointmentDate} {approvedTime}");
                return IdentityResult.Failed(new IdentityError { Description = "The selected time slot is already booked for this doctor." });
            }

            appointmentToApprove.Status = AppointmentStatus.Approved;
            appointmentToApprove.AppointmentTime = approvedTime;
            appointmentToApprove.ApprovedRejectedAt = DateTime.Now;
            _context.Update(appointmentToApprove);

            // reject other pending appointments for the same patient on the same day that might conflict
            var potentialConflictingAppointments = await _context.Appointments
                .Where(a => a.Id != appointmentToApprove.Id &&
                            a.PatientId == appointmentToApprove.PatientId && 
                            a.AppointmentDate == appointmentToApprove.AppointmentDate &&
                            a.Status == AppointmentStatus.Pending)
                .ToListAsync();

           
            var conflictingAppointments = potentialConflictingAppointments
                .Where(a => a.AppointmentTime.HasValue && Math.Abs(a.AppointmentTime.Value.Subtract(approvedTime).TotalMinutes) < 60)
                .ToList();

            foreach (var conflict in conflictingAppointments)
            {
                conflict.Status = AppointmentStatus.Rejected;
                conflict.ApprovedRejectedAt = DateTime.Now;
                _context.Update(conflict);
            }

            await _context.SaveChangesAsync();

            string formattedApprovedTime = new DateTime().Add(approvedTime).ToString("hh:mm tt");

            await SendAppointmentEmailAsync(
                appointmentToApprove.PatientEmail,
                $"Your Appointment is Approved! - {appointmentToApprove.AppointmentDate:yyyy-MM-dd}",
                $"Dear {appointmentToApprove.PatientName},\n\nYour appointment with Dr. {appointmentToApprove.Doctor?.FullName} " +
                $"({appointmentToApprove.Department?.Name} Department) on {appointmentToApprove.AppointmentDate:dddd, MMMM dd,yyyy} " +
                $"at {formattedApprovedTime} has been APPROVED.\n\nReason for appointment: {appointmentToApprove.Reason}\n\n" +
                $"We look forward to seeing you.\n\nSincerely,\nHospital Management Team"
            );

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> RejectAppointmentAsync(int appointmentId)
        {
            var appointmentToReject = await _context.Appointments
                .Include(a => a.Patient) 
                .Include(a => a.Doctor)
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointmentToReject == null || appointmentToReject.Status != AppointmentStatus.Pending)
            {
                Console.WriteLine("Could not reject appointment: not found or not pending.");
                return IdentityResult.Failed(new IdentityError { Description = "Appointment not found or not pending." });
            }

            appointmentToReject.Status = AppointmentStatus.Rejected;
            appointmentToReject.ApprovedRejectedAt = DateTime.Now;
            _context.Update(appointmentToReject);
            await _context.SaveChangesAsync();

            string formattedAppointmentTimeRejected = appointmentToReject.AppointmentTime.HasValue
                ? new DateTime().Add(appointmentToReject.AppointmentTime.Value).ToString("hh:mm tt")
                : "an unassigned time"; 

            await SendAppointmentEmailAsync(
                appointmentToReject.PatientEmail,
                $"Your Appointment Request was Rejected - {appointmentToReject.AppointmentDate:yyyy-MM-dd}",
                $"Dear {appointmentToReject.PatientName},\n\nYour appointment request for {appointmentToReject.AppointmentDate:dddd, MMMM dd,yyyy} " +
                $"with Dr. {appointmentToReject.Doctor?.FullName} ({appointmentToReject.Department?.Name} Department) " +
                $"at {formattedAppointmentTimeRejected} has been REJECTED.\n\n" +
                $"Reason: {appointmentToReject.Reason}\n\nPlease contact the hospital to reschedule or for more information.\n\nSincerely,\nHospital Management Team"
            );

            return IdentityResult.Success;
        }

        public async Task<IEnumerable<Appointment>> GetAllAppointmentsForDoctorAsync(int doctorId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)    
                .Include(a => a.Doctor)      
                .Include(a => a.Department) 
                .Where(a => a.DoctorId == doctorId) 
                .OrderByDescending(a => a.AppointmentDate) 
                .ThenByDescending(a => a.AppointmentTime)  
                .ToListAsync();
        }

        public async Task<bool> UpdateAppointmentStatusAsync(int id, AppointmentStatus newStatus)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return false;

            appointment.Status = newStatus;
            _context.Entry(appointment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompleteAppointmentAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                                            .Include(a => a.Patient) // Include Patient to  use their email later if needed for notifications
                                            .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                
                return false;
            }

            if (appointment.Status == AppointmentStatus.Approved) // Only complete if currently Approved
            {
                appointment.Status = AppointmentStatus.Completed;
               
                _context.Entry(appointment).State = EntityState.Modified;
                await _context.SaveChangesAsync();

              

                return true;
            }
            else
            {
               
                return false;
            }
        }

        public async Task<Appointment?> GetAppointmentByBillIdAsync(int billId) 
        {
            
            return await _context.Appointments
                                 .Include(a => a.Bill)
                                 .FirstOrDefaultAsync(a => a.Bill != null && a.Bill.BillId == billId);
        }

       
        public async Task<IEnumerable<Appointment>> GetAllAppointmentsAsync() //
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Department) 
                .Include(a => a.Department) 
                .Include(a => a.Bill)       
                .OrderByDescending(a => a.AppointmentDate) 
                .ThenByDescending(a => a.AppointmentTime)  
                .ToListAsync();
        }


        private async Task SendAppointmentEmailAsync(string toEmail, string subject, string plainTextContent)
        {
            var apiKey = _configuration["SendGridSettings:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("SendGrid API Key is not configured. Email will not be sent.");
                return;
            }

            var client = new SendGridClient(apiKey);
            var fromEmail = new EmailAddress("no-reply@yourhospital.com", "Hospital Management"); // Replace with a verified sender email in SendGrid
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(fromEmail, to, subject, plainTextContent, plainTextContent);

            try
            {
                var response = await client.SendEmailAsync(msg);
                Console.WriteLine($"Email sent to {toEmail} with status code: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"SendGrid response error: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {toEmail}: {ex.Message}");
            }
        }
    }
}