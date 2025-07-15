using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace HospitalManagementSystem.Controllers
{
    [Route("Appointments/[action]")]
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IDoctorAccountService _doctorAccountService;
        private readonly HospitalManagementSystem.Repository.Data.ApplicationDbContext _context;

        public AppointmentsController(IAppointmentService appointmentService,
                                      IDoctorAccountService doctorAccountService,
                                      HospitalManagementSystem.Repository.Data.ApplicationDbContext context)
        {
            _appointmentService = appointmentService;
            _doctorAccountService = doctorAccountService;
            _context = context;
        }

        
        [HttpGet]
        public async Task<IActionResult> BookAppointment()
        {
            try
            {
               
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized("User ID not found.");
                }

                
                var departments = await _appointmentService.GetAllDepartmentsAsync();
                var doctors = await _appointmentService.GetDoctorsByDepartmentAsync(0);
                var appointmentDates = GetNext30DaysDates();
                var availableTimeSlots = new List<TimeSpan>();

                ViewBag.Departments = new SelectList(departments, "Id", "Name");
                ViewBag.Doctors = new SelectList(doctors, "Id", "FullName");
                ViewBag.AppointmentDates = appointmentDates;
                ViewBag.AvailableTimeSlots = new SelectList(availableTimeSlots);

                
                var patient = await _appointmentService.GetPatientByUserIdAsync(userId);

                if (patient != null)
                {
                   
                    var appointment = new Appointment
                    {
                        PatientId = patient.PatientId,
                        PatientName = patient.Name,
                        PatientEmail = patient.Email,
                        PatientPhoneNumber = patient.PhoneNumber
                    };

                    return View(appointment);
                }
                else
                {
                    
                    return View(new Appointment());
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error in BookAppointment: {ex.Message}");
                return View(new Appointment());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(Appointment appointment)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized("User ID not found.");
                }

                var patient = await _appointmentService.GetPatientByUserIdAsync(userId);
                if (patient == null)
                {
                    ModelState.AddModelError("", "Patient profile not found.");
                    return View(appointment);
                }

                // setting patient details from profile
                appointment.PatientId = patient.PatientId;
                appointment.PatientName = patient.Name;
                appointment.PatientEmail = patient.Email;
                appointment.PatientPhoneNumber = patient.PhoneNumber;

                if (!ModelState.IsValid)
                {
                    
                    ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name");
                    ViewBag.Doctors = new SelectList(await _appointmentService.GetDoctorsByDepartmentAsync(appointment.DepartmentId ?? 0), "Id", "FullName");
                    ViewBag.AppointmentDates = GetNext30DaysDates();
                    ViewBag.AvailableTimeSlots = new SelectList(new List<TimeSpan>());
                    return View(appointment);
                }

                var createdAppointment = await _appointmentService.CreateAppointmentAsync(appointment, true);
                if (createdAppointment != null)
                {
                    TempData["SuccessMessage"] = "Your appointment has been booked successfully!";
                    return RedirectToAction(nameof(BookAppointment));
                }
                else
                {
                    ModelState.AddModelError("", "The selected time slot is not available. Please choose another time.");
                    ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name");
                    ViewBag.Doctors = new SelectList(await _appointmentService.GetDoctorsByDepartmentAsync(appointment.DepartmentId ?? 0), "Id", "FullName");
                    ViewBag.AppointmentDates = GetNext30DaysDates();
                    ViewBag.AvailableTimeSlots = new SelectList(new List<TimeSpan>());
                    return View(appointment);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while booking your appointment. Please try again.");
                Console.WriteLine($"Error creating appointment: {ex.Message}");
                ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name");
                ViewBag.Doctors = new SelectList(await _appointmentService.GetDoctorsByDepartmentAsync(appointment.DepartmentId ?? 0), "Id", "FullName");
                ViewBag.AppointmentDates = GetNext30DaysDates();
                ViewBag.AvailableTimeSlots = new SelectList(new List<TimeSpan>());
                return View(appointment);
            }
        }

        private List<SelectListItem> GetNext30DaysDates()
        {
            var dates = new List<SelectListItem>();
            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.Today.AddDays(i);
                dates.Add(new SelectListItem
                {
                    Value = date.ToString("yyyy-MM-dd"),
                    Text = date.ToString("dddd, MMM dd,yyyy")
                });
            }
            return dates;
        }

        
        [HttpGet]
        public async Task<JsonResult> GetDoctorsByDepartment(int departmentId)
        {
            var doctors = await _appointmentService.GetDoctorsByDepartmentAsync(departmentId);
            return Json(doctors.Select(d => new
            {
                d.Id,
                d.FullName,
                WorkingHoursStart = d.WorkingHoursStart?.ToString("hh\\:mm"),
                WorkingHoursEnd = d.WorkingHoursEnd?.ToString("hh\\:mm")
            }));
        }

        [HttpGet]
        public async Task<JsonResult> GetDoctorAvailableTimeSlots(int doctorId, string appointmentDate)
        {
            if (!DateTime.TryParse(appointmentDate, out DateTime date))
            {
                return Json(new List<object>());
            }

            var availableSlots = await _appointmentService.GetAvailableTimeSlotsAsync(doctorId, date);

            return Json(availableSlots.Select(ts => {
                var dateTime = date.Date.Add(ts);
                return new
                {
                    Value = dateTime.ToString("HH:mm"),
                    Text = dateTime.ToString("hh:mm tt")
                };
            }));
        }

        
        public IActionResult BookingConfirmation()
        {
            return View();
        }

        
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> DoctorDashboard(int? doctorId, int? departmentFilterId)
        {
            if (User.IsInRole("Doctor"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctorProfile = await _doctorAccountService.GetDoctorProfileByUserIdAsync(userId);
                if (doctorProfile != null)
                {
                    doctorId = doctorProfile.Id;
                }
            }

            ViewBag.Doctors = new SelectList(await _appointmentService.GetAllDoctorsAsync(), "Id", "FullName", doctorId);
            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name", departmentFilterId);

            List<Appointment> pendingAppointments = new List<Appointment>();

            if (doctorId.HasValue)
            {
                pendingAppointments = await _appointmentService.GetPendingAppointmentsForDoctorAsync(doctorId.Value);
            }
            else if (departmentFilterId.HasValue)
            {
                pendingAppointments = await _appointmentService.GetPendingAppointmentsForDepartmentOnlyAsync(departmentFilterId.Value);
            }

            return View(pendingAppointments);
        }

        // POST   -  Appointments/Approve (Doctor approves an appointment and sets the time)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Approve(int id, string approvedTimeString)
        {
            if (!TimeSpan.TryParse(approvedTimeString, out TimeSpan approvedTime))
            {
                TempData["Message"] = "Invalid time format provided.";
                var currentAppointmentForRedirect = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
                return RedirectToAction(nameof(DoctorDashboard), new { doctorId = currentAppointmentForRedirect?.DoctorId, departmentFilterId = currentAppointmentForRedirect?.DepartmentId });
            }

            var currentAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            int? doctorIdForRedirect = currentAppointment?.DoctorId;
            int? departmentIdForRedirect = currentAppointment?.DepartmentId;

            var result = await _appointmentService.ApproveAppointmentAsync(id, approvedTime);

            if (result.Succeeded)
            {
                TempData["Message"] = "Appointment approved successfully!";
            }
            else
            {
                TempData["Message"] = $"Failed to approve appointment: {result.Errors.FirstOrDefault()?.Description ?? "Unknown error."}";
            }

            if (doctorIdForRedirect.HasValue)
            {
                return RedirectToAction(nameof(DoctorDashboard), new { doctorId = doctorIdForRedirect });
            }
            else if (departmentIdForRedirect.HasValue)
            {
                return RedirectToAction(nameof(DoctorDashboard), new { departmentFilterId = departmentIdForRedirect });
            }
            return RedirectToAction(nameof(DoctorDashboard));
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var currentAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            int? doctorIdForRedirect = currentAppointment?.DoctorId;
            int? departmentIdForRedirect = currentAppointment?.DepartmentId;

            var result = await _appointmentService.RejectAppointmentAsync(id);

            if (result.Succeeded)
            {
                TempData["Message"] = "Appointment rejected successfully.";
            }
            else
            {
                TempData["Message"] = $"Failed to reject appointment: {result.Errors.FirstOrDefault()?.Description ?? "Unknown error."}";
            }

            if (doctorIdForRedirect.HasValue)
            {
                return RedirectToAction(nameof(DoctorDashboard), new { doctorId = doctorIdForRedirect });
            }
            else if (departmentIdForRedirect.HasValue)
            {
                return RedirectToAction(nameof(DoctorDashboard), new { departmentFilterId = departmentIdForRedirect });
            }
            return RedirectToAction(nameof(DoctorDashboard));
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            var currentAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            int? doctorIdForRedirect = currentAppointment?.DoctorId;
            int? departmentIdForRedirect = currentAppointment?.DepartmentId;
            string patientEmailForRedirect = currentAppointment?.PatientEmail;
            string patientPhoneForRedirect = currentAppointment?.PatientPhoneNumber;

            var result = await _appointmentService.CancelAppointmentAsync(id);

            if (result.Succeeded)
            {
                TempData["Message"] = "Appointment cancelled successfully.";
            }
            else
            {
                TempData["Message"] = $"Failed to cancel appointment: {result.Errors.FirstOrDefault()?.Description ?? "Unknown error."}";
            }

            if (Request.Headers["Referer"].ToString().Contains("DoctorDashboard"))
            {
                if (User.IsInRole("Doctor"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var doctorProfile = await _doctorAccountService.GetDoctorProfileByUserIdAsync(userId);
                    if (doctorProfile != null)
                    {
                        return RedirectToAction(nameof(DoctorDashboard), new { doctorId = doctorProfile.Id });
                    }
                }
                if (doctorIdForRedirect.HasValue)
                {
                    return RedirectToAction(nameof(DoctorDashboard), new { doctorId = doctorIdForRedirect });
                }
                else if (departmentIdForRedirect.HasValue)
                {
                    return RedirectToAction(nameof(DoctorDashboard), new { departmentFilterId = departmentIdForRedirect });
                }
                return RedirectToAction(nameof(DoctorDashboard));
            }
            else if (Request.Headers["Referer"].ToString().Contains("PatientAppointments"))
            {
                return RedirectToAction("PatientAppointments", new { email = patientEmailForRedirect, phoneNumber = patientPhoneForRedirect });
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
