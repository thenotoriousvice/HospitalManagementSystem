using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System; // Make sure System is imported for DateTime

namespace HospitalManagementSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IDoctorAccountService _doctorAccountService; // Inject DoctorAccountService
        private readonly HospitalManagementSystem.Repository.Data.ApplicationDbContext _context; // For robust ID lookup

        public AppointmentsController(IAppointmentService appointmentService,
                                      IDoctorAccountService doctorAccountService, // Inject new service
                                      HospitalManagementSystem.Repository.Data.ApplicationDbContext context)
        {
            _appointmentService = appointmentService;
            _doctorAccountService = doctorAccountService; // Initialize
            _context = context;
        }

        // GET: Appointments/Create (Patient Booking Form)
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name");
            ViewBag.Doctors = new SelectList(new List<Doctor>(), "Id", "FullName"); // Use FullName
            ViewBag.AppointmentDates = GetNext30DaysDates();
            ViewBag.AvailableTimeSlots = new SelectList(new List<TimeSpan>());

            // *** FIX: Explicitly specify the view name "BookAppointment" ***
            return View("BookAppointment", new Appointment());
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

        // AJAX endpoint to get doctors by department (includes working hours now)
        [HttpGet]
        public async Task<JsonResult> GetDoctorsByDepartment(int departmentId)
        {
            var doctors = await _appointmentService.GetDoctorsByDepartmentAsync(departmentId);
            return Json(doctors.Select(d => new
            {
                d.Id,
                d.FullName, // Use FullName
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

            // Corrected Code: Convert TimeSpan to DateTime before formatting
            return Json(availableSlots.Select(ts => {
                // Create a full DateTime object to correctly format the time of day
                var dateTime = date.Date.Add(ts);
                return new
                {
                    // Use 24-hour format for the value field, which is easier to parse on form submission
                    Value = dateTime.ToString("HH:mm"),
                    // Use 12-hour format with AM/PM for the display text
                    Text = dateTime.ToString("hh:mm tt")
                };
            }));
        }

        // POST: Appointments/Create (Submit Patient Booking)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment, bool isExistingPatient)
        {
            ModelState.Remove(nameof(appointment.Patient));
            ModelState.Remove(nameof(appointment.Doctor));
            ModelState.Remove(nameof(appointment.Department));

            if (!appointment.DoctorId.HasValue && !appointment.DepartmentId.HasValue)
            {
                ModelState.AddModelError("", "Please select a Doctor or Department.");
            }

            if (!isExistingPatient)
            {
                if (string.IsNullOrWhiteSpace(appointment.PatientName))
                {
                    ModelState.AddModelError(nameof(appointment.PatientName), "The Patient Name field is required.");
                }
                if (string.IsNullOrWhiteSpace(appointment.PatientEmail))
                {
                    ModelState.AddModelError(nameof(appointment.PatientEmail), "The Patient Email field is required.");
                }
                else if (!new EmailAddressAttribute().IsValid(appointment.PatientEmail))
                {
                    ModelState.AddModelError(nameof(appointment.PatientEmail), "The Patient Email field is not a valid e-mail address.");
                }
                if (string.IsNullOrWhiteSpace(appointment.PatientPhoneNumber))
                {
                    ModelState.AddModelError(nameof(appointment.PatientPhoneNumber), "The Patient Phone Number field is required.");
                }
                else if (!new PhoneAttribute().IsValid(appointment.PatientPhoneNumber))
                {
                    ModelState.AddModelError(nameof(appointment.PatientPhoneNumber), "The Patient Phone Number field is not a valid phone number.");
                }
            }
            else
            {
                if (!appointment.PatientId.HasValue)
                {
                    ModelState.AddModelError(nameof(appointment.PatientId), "Please provide an existing Patient ID, or select 'No' if you are a new patient.");
                }
                ModelState.Remove(nameof(appointment.PatientName));
                ModelState.Remove(nameof(appointment.PatientEmail));
                ModelState.Remove(nameof(appointment.PatientPhoneNumber));
            }

            if (!appointment.AppointmentTime.HasValue)
            {
                ModelState.AddModelError(nameof(appointment.AppointmentTime), "Please select a preferred time slot.");
            }


            if (ModelState.IsValid)
            {
                var createdAppointment = await _appointmentService.CreateAppointmentAsync(appointment, isExistingPatient);
                if (createdAppointment != null)
                {
                    TempData["SuccessMessage"] = "Your appointment request has been submitted successfully!";
                    return RedirectToAction(nameof(BookingConfirmation));
                }
                else
                {
                    ModelState.AddModelError("", "The selected time slot is not available for any doctor in the chosen department or for the selected doctor. Please choose another time slot.");
                }
            }

            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name", appointment.DepartmentId);
            List<Doctor> doctors = new List<Doctor>();
            if (appointment.DepartmentId.HasValue)
            {
                doctors = (await _appointmentService.GetDoctorsByDepartmentAsync(appointment.DepartmentId.Value)).ToList();
                ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", appointment.DoctorId); // Use FullName
            }
            else
            {
                ViewBag.Doctors = new SelectList(new List<Doctor>(), "Id", "FullName");
            }
            ViewBag.AppointmentDates = GetNext30DaysDates();

            if (appointment.DoctorId.HasValue && appointment.AppointmentDate != DateTime.MinValue)
            {
                ViewBag.AvailableTimeSlots = new SelectList(await _appointmentService.GetAvailableTimeSlotsAsync(appointment.DoctorId.Value, appointment.AppointmentDate), "ToString", "ToString", appointment.AppointmentTime?.ToString("hh\\:mm"));
            }
            else
            {
                ViewBag.AvailableTimeSlots = new SelectList(new List<TimeSpan>());
            }
            // *** FIX: Explicitly specify the view name "BookAppointment" when returning the view with model errors ***
            return View("BookAppointment", appointment);
        }

        // GET: Appointments/BookingConfirmation
        public IActionResult BookingConfirmation()
        {
            return View();
        }

        // GET: Appointments/DoctorDashboard (Displays pending appointments for a doctor/department)
        [Authorize(Roles = "Doctor,Admin")] // Only authenticated Doctors and Admins can access
        public async Task<IActionResult> DoctorDashboard(int? doctorId, int? departmentFilterId)
        {
            // If a doctor is logged in and no specific doctorId is provided for filter, default to their own appointments
            if (User.IsInRole("Doctor"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var doctorProfile = await _doctorAccountService.GetDoctorProfileByUserIdAsync(userId);
                if (doctorProfile != null)
                {
                    doctorId = doctorProfile.Id;
                }
            }

            ViewBag.Doctors = new SelectList(await _appointmentService.GetAllDoctorsAsync(), "Id", "FullName", doctorId); // Use FullName
            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name", departmentFilterId);

            List<Appointment> pendingAppointments = new List<Appointment>();

            // The Dashboard in DoctorProfileController uses GetAppointmentsForDoctorAsync
            // This AppointmentsController.DoctorDashboard uses specific GetPendingAppointmentsForDoctorAsync and GetPendingAppointmentsForDepartmentOnlyAsync
            // This is acceptable if the goal of THIS dashboard is strictly pending appointments.
            if (doctorId.HasValue)
            {
                pendingAppointments = await _appointmentService.GetPendingAppointmentsForDoctorAsync(doctorId.Value);
            }
            else if (departmentFilterId.HasValue)
            {
                // This case handles pending appointments not yet assigned to a specific doctor in a department
                pendingAppointments = await _appointmentService.GetPendingAppointmentsForDepartmentOnlyAsync(departmentFilterId.Value);
            }

            // If neither doctorId nor departmentFilterId is specified, and it's an admin, show all pending appointments,
            // or if it's a doctor without a profile, show nothing.
            // Current logic implies if no filter, an empty list is shown for pending appointments.
            return View(pendingAppointments);
        }

        // POST: Appointments/Approve (Doctor approves an appointment and sets the time)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Approve(int id, string approvedTimeString)
        {
            if (!TimeSpan.TryParse(approvedTimeString, out TimeSpan approvedTime))
            {
                TempData["Message"] = "Invalid time format provided.";
                // Try to get current appointment for redirection even if time parsing fails
                var currentAppointmentForRedirect = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
                return RedirectToAction(nameof(DoctorDashboard), new { doctorId = currentAppointmentForRedirect?.DoctorId, departmentFilterId = currentAppointmentForRedirect?.DepartmentId });
            }

            var currentAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            int? doctorIdForRedirect = currentAppointment?.DoctorId;
            int? departmentIdForRedirect = currentAppointment?.DepartmentId;

            // *** FIX: Check the Succeeded property of IdentityResult ***
            var result = await _appointmentService.ApproveAppointmentAsync(id, approvedTime);

            if (result.Succeeded)
            {
                TempData["Message"] = "Appointment approved successfully!";
            }
            else
            {
                TempData["Message"] = $"Failed to approve appointment: {result.Errors.FirstOrDefault()?.Description ?? "Unknown error."}";
            }

            // Redirect back to the Dashboard, preserving filters if possible
            if (doctorIdForRedirect.HasValue)
            {
                return RedirectToAction(nameof(DoctorDashboard), new { doctorId = doctorIdForRedirect });
            }
            else if (departmentIdForRedirect.HasValue)
            {
                return RedirectToAction(nameof(DoctorDashboard), new { departmentFilterId = departmentIdForRedirect });
            }
            else
            {
                return RedirectToAction(nameof(DoctorDashboard));
            }
        }

        // POST: Appointments/Reject (Doctor rejects an appointment)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var currentAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            int? doctorIdForRedirect = currentAppointment?.DoctorId;
            int? departmentIdForRedirect = currentAppointment?.DepartmentId;

            // *** FIX: Check the Succeeded property of IdentityResult ***
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
            else
            {
                return RedirectToAction(nameof(DoctorDashboard));
            }
        }

        // POST: Appointments/Cancel (For both doctor and patient)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Any authenticated user can cancel their own appointments, or doctors/admins can cancel others
        public async Task<IActionResult> Cancel(int id)
        {
            var currentAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            int? doctorIdForRedirect = currentAppointment?.DoctorId;
            int? departmentIdForRedirect = currentAppointment?.DepartmentId;
            string patientEmailForRedirect = currentAppointment?.PatientEmail;
            string patientPhoneForRedirect = currentAppointment?.PatientPhoneNumber;

            // *** FIX: Check the Succeeded property of IdentityResult ***
            var result = await _appointmentService.CancelAppointmentAsync(id);

            if (result.Succeeded)
            {
                TempData["Message"] = "Appointment cancelled successfully.";
            }
            else
            {
                TempData["Message"] = $"Failed to cancel appointment: {result.Errors.FirstOrDefault()?.Description ?? "Unknown error."}";
            }

            // Determine redirect based on who is cancelling (simple heuristic for now)
            // If from doctor dashboard, try to redirect back with doctor/department filter
            if (Request.Headers["Referer"].ToString().Contains("DoctorDashboard"))
            {
                if (User.IsInRole("Doctor")) // If a doctor is cancelling, redirect to their dashboard
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var doctorProfile = await _doctorAccountService.GetDoctorProfileByUserIdAsync(userId);
                    if (doctorProfile != null)
                    {
                        return RedirectToAction(nameof(DoctorDashboard), new { doctorId = doctorProfile.Id });
                    }
                }
                // Fallback for admin or general dashboard view
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
            // If from patient appointments, try to redirect back with patient details
            else if (Request.Headers["Referer"].ToString().Contains("PatientAppointments"))
            {
                // This redirect assumes a PatientAppointments Index action might exist
                // in the same controller or a different one.
                // Assuming `PatientAppointments` action takes email and phone as parameters.
                return RedirectToAction("PatientAppointments", new { email = patientEmailForRedirect, phoneNumber = patientPhoneForRedirect });
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
