using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Added for SelectList
using HospitalManagementSystem.Repository.Models; // Added for DoctorRegistrationViewModel and Department

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IAppointmentService _appointmentService; // Injected: IAppointmentService
        private readonly IDoctorAccountService _doctorAccountService; // Injected: IDoctorAccountService

        public AdminController(
            IPatientService patientService,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IAppointmentService appointmentService, // Constructor parameter
            IDoctorAccountService doctorAccountService) // Constructor parameter
        {
            _patientService = patientService;
            _userManager = userManager;
            _signInManager = signInManager;
            _appointmentService = appointmentService; // Assigned
            _doctorAccountService = doctorAccountService; // Assigned
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                IdentityUser user = null;
                try
                {
                    // Try to find user by username (which is email during registration) or by email
                    user = await _userManager.FindByNameAsync(model.ContactNumberOrEmail);
                    if (user == null)
                    {
                        user = await _userManager.FindByEmailAsync(model.ContactNumberOrEmail);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception (e.g., using ILogger)
                    Console.WriteLine($"Error finding user in Login: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "An error occurred while trying to find your account. Please try again.");
                    return View(model);
                }


                if (user != null)
                {
                    try
                    {
                        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

                        // Check if the user is an Admin
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            if (Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }
                            return RedirectToAction("Dashboard", "Admin"); // Redirect Admin to Admin Dashboard
                        }

                        // If login failed (e.g., wrong password or locked out)
                        else
                        {
                            await _signInManager.SignOutAsync(); // Ensure previous sessions are cleared
                            if (result.IsLockedOut)
                            {
                                ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
                            }
                            else if (result.IsNotAllowed)
                            {
                                ModelState.AddModelError(string.Empty, "Login not allowed. Your account may require email confirmation.");
                            }
                            else if (result.RequiresTwoFactor)
                            {
                                ModelState.AddModelError(string.Empty, "Two-factor authentication required.");
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your username/email and password.");
                            }
                            return View(model);
                        }
                    }


                    catch (Exception ex)
                    {
                        // Log the exception (e.g., using ILogger)
                        Console.WriteLine($"Error during PasswordSignInAsync: {ex.Message}");
                        ModelState.AddModelError(string.Empty, "An unexpected error occurred during login. Please try again.");
                        return View(model);
                    }
                }
                // If user is null (not found)
                else
                {
                    await _signInManager.SignOutAsync(); // Ensure previous sessions are cleared
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Account not found.");
                }
            }
            return View(model);
        }

        public IActionResult Dashboard()
        {
            ViewData["Title"] = "Admin Dashboard";
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Doctors() // ONLY ONE INSTANCE REMAINS
        {
            ViewData["Title"] = "Manage Doctors";
            var doctors = await _doctorAccountService.GetAllDoctorsAsync();
            return View(doctors);
        }

        // Action for managing appointments (replaces the placeholder)
        public async Task<IActionResult> Appointments()
        {
            // Fetch all appointments including related data
            var allAppointments = await _appointmentService.GetAllAppointmentsAsync();

            // Transform to DoctorAppointmentViewModel to include Bill information
            var allDoctorAppointmentViewModels = allAppointments.Select(a => new DoctorAppointmentViewModel
            {
                Appointment = a,
                Bill = a.Bill // Populate the Bill property
            }).ToList();

            // Categorize into upcoming and past appointments
            var upcomingAppointments = allDoctorAppointmentViewModels
                .Where(a => a.Appointment.AppointmentDate.Date >= DateTime.Today.Date)
                .OrderBy(a => a.Appointment.AppointmentDate)
                .ThenBy(a => a.Appointment.AppointmentTime)
                .ToList();

            var pastAppointments = allDoctorAppointmentViewModels
                .Where(a => a.Appointment.AppointmentDate.Date < DateTime.Today.Date)
                .OrderByDescending(a => a.Appointment.AppointmentDate)
                .ThenByDescending(a => a.Appointment.AppointmentTime)
                .ToList();

            // Pass categorized lists to the view using ViewBag
            ViewBag.UpcomingAppointments = upcomingAppointments;
            ViewBag.PastAppointments = pastAppointments;

            // No need to pass a model if using ViewBag extensively
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var result = await _appointmentService.CancelAppointmentAsync(id);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Appointment cancelled successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Error cancelling appointment: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }
            return RedirectToAction("Appointments");
        }

        [HttpPost]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var result = await _appointmentService.CompleteAppointmentAsync(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Appointment marked as completed.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error marking appointment as completed.";
            }
            return RedirectToAction("Appointments");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, AppointmentStatus newStatus)
        {
            var result = await _appointmentService.UpdateAppointmentStatusAsync(id, newStatus);
            if (result)
            {
                TempData["SuccessMessage"] = $"Appointment status updated to {newStatus.ToString().Replace("_", " ")}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error updating appointment status.";
            }
            return RedirectToAction("Appointments");
        }

        // GET: /Admin/RegisterDoctor
        public async Task<IActionResult> RegisterDoctor()
        {
            ViewData["Title"] = "Register New Doctor";
            // Populate ViewBag.Departments with actual data
            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name");

            // Ensure the view receives a new DoctorRegistrationViewModel
            return View("~/Views/Admin/RegisterDoctor.cshtml", new DoctorRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctor(DoctorRegistrationViewModel model) // Model type changed
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Doctor");

                    // Save the rest of the doctor's profile details
                    var doctorProfile = new Doctor
                    {
                        IdentityUserId = user.Id,
                        FullName = model.FullName,
                        PhoneNumber = model.PhoneNumber,
                        Qualification = model.Qualification,
                        ExperienceYears = model.ExperienceYears,
                        DepartmentId = model.DepartmentId
                    };

                    // Assuming CreateDoctorProfileAsync handles adding a new Doctor entity to DB
                    var doctorSaveResult = await _doctorAccountService.CreateDoctorProfileAsync(doctorProfile);

                    if (doctorSaveResult) // Assuming CreateDoctorProfileAsync returns a bool or similar success indicator
                    {
                        TempData["Message"] = "Doctor registered successfully!";
                        ModelState.Clear();
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        // Rollback IdentityUser creation if doctor profile saving fails
                        await _userManager.DeleteAsync(user);
                        ModelState.AddModelError(string.Empty, "Doctor user created, but profile details could not be saved. Please try again.");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            ViewData["Title"] = "Register New Doctor";
            // Repopulate ViewBag.Departments if returning to view due to validation errors
            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name", model.DepartmentId);
            return View("~/Views/Admin/RegisterDoctor.cshtml", model);
        }

        [AllowAnonymous]
        public IActionResult RegisterAdmin()
        {
            ViewData["Title"] = "Register New Admin";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin(UserRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    TempData["SuccessMessage"] = "Admin registered successfully! You can now log in with these credentials.";
                    ModelState.Clear();
                    return RedirectToAction(nameof(RegisterAdmin));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            ViewData["Title"] = "Register New Admin";
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            // Assuming this is for patient management from Admin perspective
            var patients = await _patientService.GetAllPatients();
            return View(patients);
        }
    }
}