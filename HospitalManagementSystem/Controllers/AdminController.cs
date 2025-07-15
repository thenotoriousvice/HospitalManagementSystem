using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; 
using HospitalManagementSystem.Repository.Models; 
namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IAppointmentService _appointmentService; 
        private readonly IDoctorAccountService _doctorAccountService; 

        public AdminController(
            IPatientService patientService,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IAppointmentService appointmentService, 
            IDoctorAccountService doctorAccountService) 
        {
            _patientService = patientService;
            _userManager = userManager;
            _signInManager = signInManager;
            _appointmentService = appointmentService;
            _doctorAccountService = doctorAccountService; 
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
                    // user by username or by email
                    user = await _userManager.FindByNameAsync(model.ContactNumberOrEmail);
                    if (user == null)
                    {
                        user = await _userManager.FindByEmailAsync(model.ContactNumberOrEmail);
                    }
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine($"Error finding user in Login: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "An error occurred while trying to find your account. Please try again.");
                    return View(model);
                }


                if (user != null)
                {
                    try
                    {
                        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

                        
                        if (result.Succeeded) 
                        {
                            
                            if (await _userManager.IsInRoleAsync(user, "Admin"))
                            {
                                if (Url.IsLocalUrl(returnUrl))
                                {
                                    return Redirect(returnUrl);
                                }
                                return RedirectToAction("Dashboard", "Admin"); 
                            }
                            else
                            {
                                // If the user is found and password is correct, but they are not an Admin, sign them out
                                await _signInManager.SignOutAsync();
                                ModelState.AddModelError(string.Empty, "You do not have administrative privileges.");
                                return View(model);
                            }
                        }
                       
                        else
                        {
                            await _signInManager.SignOutAsync(); 
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
                        
                        Console.WriteLine($"Error during PasswordSignInAsync: {ex.Message}");
                        ModelState.AddModelError(string.Empty, "An unexpected error occurred during login. Please try again.");
                        return View(model);
                    }
                }
               
                else
                {
                    await _signInManager.SignOutAsync(); 
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
        public async Task<IActionResult> Doctors() 
        {
            ViewData["Title"] = "Manage Doctors";
            var doctors = await _doctorAccountService.GetAllDoctorsAsync();
            return View(doctors);
        }

        
        public async Task<IActionResult> Appointments()
        {
            
            var allAppointments = await _appointmentService.GetAllAppointmentsAsync();

            
            var allDoctorAppointmentViewModels = allAppointments.Select(a => new DoctorAppointmentViewModel
            {
                Appointment = a,
                Bill = a.Bill 
            }).ToList();

            
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

           
            ViewBag.UpcomingAppointments = upcomingAppointments;
            ViewBag.PastAppointments = pastAppointments;

           
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

        // GET --> /Admin/RegisterDoctor
        public async Task<IActionResult> RegisterDoctor()
        {
            ViewData["Title"] = "Register New Doctor";
            
            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name");

           
            return View("~/Views/Admin/RegisterDoctor.cshtml", new DoctorRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctor(DoctorRegistrationViewModel model) 
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true, PhoneNumber = model.PhoneNumber };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Doctor");

                    
                    var doctorProfile = new Doctor
                    {
                        IdentityUserId = user.Id,
                        FullName = model.FullName,
                        PhoneNumber = model.PhoneNumber,
                        Qualification = model.Qualification,
                        ExperienceYears = model.ExperienceYears,
                        DepartmentId = model.DepartmentId
                    };

                   
                    var doctorSaveResult = await _doctorAccountService.CreateDoctorProfileAsync(doctorProfile);

                    if (doctorSaveResult) 
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
           
            var patients = await _patientService.GetAllPatients();
            return View(patients);
        }
    }
}