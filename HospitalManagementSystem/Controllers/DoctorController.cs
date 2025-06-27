// --- Combined Using Statements ---
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Models; // For custom project models and ViewModels
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using Microsoft.AspNetCore.Authorization; // For Authorize and AllowAnonymous attributes
using System.Security.Claims; // For accessing User.FindFirstValue
using System; // For TimeSpan
using System.Linq; // For LINQ operations
using System.Collections.Generic;
using HospitalManagementSystem.ViewModels; // For List<T>

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Doctor")] // All actions require Doctor role by default
    public class DoctorController : Controller
    {
        // --- Combined Dependencies ---
        private readonly IDoctorAccountService _doctorAccountService;
        private readonly IAppointmentService _appointmentService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        // --- Updated Constructor with all Dependencies ---
        public DoctorController(
            IDoctorAccountService doctorAccountService,
            IAppointmentService appointmentService,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _doctorAccountService = doctorAccountService;
            _appointmentService = appointmentService;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // =====================================================================
        // == ANONYMOUS/PUBLIC ACTIONS (from original AccountController) ==
        // =====================================================================

        // GET: /Doctor/Register
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            ViewBag.Departments = new SelectList(await _doctorAccountService.GetAllDepartmentsAsync(), "Id", "Name");
            return View();
        }

        // POST: /Doctor/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DoctorRegistrationViewModel model)
        {
            ModelState.Remove("DoctorId"); // It's database-generated

            if (ModelState.IsValid)
            {
                if (await _userManager.FindByEmailAsync(model.Email) != null)
                {
                    ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                    ViewBag.Departments = new SelectList(await _doctorAccountService.GetAllDepartmentsAsync(), "Id", "Name", model.DepartmentId);
                    return View(model);
                }

                var result = await _doctorAccountService.RegisterDoctorAsync(model);
                if (result.Succeeded)
                {
                    // --- IMPORTANT CHANGE: Automatically sign in the newly registered doctor ---
                    var newUser = await _userManager.FindByEmailAsync(model.Email);
                    if (newUser != null)
                    {
                        // Ensure the "Doctor" role is assigned here or within RegisterDoctorAsync
                        // If it's done in RegisterDoctorAsync, this SignInAsync will correctly authenticate them.
                        // If RegisterDoctorAsync doesn't assign the role, you MUST ensure it does.
                        // Assuming RegisterDoctorAsync has already assigned the role.
                        await _signInManager.SignInAsync(newUser, isPersistent: false);
                        TempData["SuccessMessage"] = "Doctor account created and you are now logged in! Please complete your profile details.";
                        return RedirectToAction(nameof(Dashboard)); // Redirect to Doctor Dashboard
                    }
                    else
                    {
                        // Fallback if user somehow not found immediately after creation (unlikely)
                        TempData["SuccessMessage"] = "Doctor account created. Please log in with your new credentials.";
                        return RedirectToAction(nameof(Login)); // Redirect to login page
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            ViewBag.Departments = new SelectList(await _doctorAccountService.GetAllDepartmentsAsync(), "Id", "Name", model.DepartmentId);
            return View(model);
        }

        // GET: /Doctor/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Doctor/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null) // Using LoginViewModel from target file
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.ContactNumberOrEmail) ?? await _userManager.FindByEmailAsync(model.ContactNumberOrEmail);

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        if (await _userManager.IsInRoleAsync(user, "Doctor"))
                        {
                            if (Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }
                            // Redirect to Dashboard after successful doctor login
                            return RedirectToAction(nameof(Dashboard));
                        }
                        else
                        {
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty, "You do not have doctor privileges.");
                            return View(model);
                        }
                    }
                    else
                    {
                        await _signInManager.SignOutAsync();
                        if (result.IsLockedOut)
                        {
                            ModelState.AddModelError(string.Empty, "Account locked out.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        }
                        return View(model);
                    }
                }
                else
                {
                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            return View(model);
        }

        // POST: /Doctor/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home"); // Redirect to home page after logout
        }


        // =====================================================================
        // == AUTHORIZED DOCTOR ACTIONS (from original DoctorProfileController) ==
        // =====================================================================

        // GET: /Doctor/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _doctorAccountService.GetDoctorProfileByUserIdAsync(userId);

            // Removed the direct RedirectToAction(nameof(EditProfile)) from here.
            // Instead, we will show a message on the dashboard if the profile is incomplete.
            if (doctor == null || !doctor.WorkingHoursStart.HasValue || !doctor.WorkingHoursEnd.HasValue)
            {
                TempData["InfoMessage"] = "Welcome! Your doctor profile is incomplete. Please set your working hours by editing your profile.";
            }

            var pendingAppointments = await _appointmentService.GetAppointmentsForDoctorAsync(doctor?.Id ?? 0, null, null); // Handle null doctor

            var viewModel = new DoctorDashboardViewModel
            {
                DoctorProfile = doctor,
                PendingAppointments = pendingAppointments.Where(a => a.Status == AppointmentStatus.Pending).ToList()
            };

            return View(viewModel);
        }

        // GET: /Doctor/MyAppointments
        public async Task<IActionResult> MyAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _doctorAccountService.GetDoctorProfileByUserIdAsync(userId);
            if (doctor == null)
            {
                TempData["Message"] = "Doctor profile not found. Please complete your profile first.";
                return RedirectToAction(nameof(EditProfile));
            }

            var allAppointments = await _appointmentService.GetAllAppointmentsForDoctorAsync(doctor.Id);
            return View(allAppointments);
        }

        // GET: /Doctor/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var doctor = await _doctorAccountService.GetDoctorProfileByUserIdAsync(userId);
            EditProfileViewModel model;

            if (doctor == null)
            {
                TempData["Message"] = "Please complete your doctor profile.";
                model = new EditProfileViewModel
                {
                    Email = User.FindFirstValue(ClaimTypes.Email),
                    DoctorId = 0
                };
                ViewBag.st = "Create Profile";
            }
            else
            {
                model = new EditProfileViewModel
                {
                    DoctorId = doctor.Id,
                    FullName = doctor.FullName,
                    Email = doctor.IdentityUser?.Email,
                    PhoneNumber = doctor.PhoneNumber,
                    Qualification = doctor.Qualification,
                    ExperienceYears = doctor.ExperienceYears,
                    DepartmentId = doctor.DepartmentId ?? 0,
                    WorkingHoursStart = doctor.WorkingHoursStart,
                    WorkingHoursEnd = doctor.WorkingHoursEnd
                };
                ViewBag.st = "Save Changes";
            }
            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name", model.DepartmentId);
            return View(model);
        }

        // POST: /Doctor/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            ModelState.Remove(nameof(model.Email)); // Email is part of IdentityUser, not Doctor profile for update
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name", model.DepartmentId);
                ViewBag.st = model.DoctorId == 0 ? "Create Profile" : "Save Changes";
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, "Could not find user identity. Please log in again.");
                return View(model);
            }

            var result = await _doctorAccountService.UpdateDoctorProfileAsync(model, userId);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Dashboard)); // Redirect to Dashboard after profile is complete
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Departments = new SelectList(await _appointmentService.GetAllDepartmentsAsync(), "Id", "Name", model.DepartmentId);
            ViewBag.st = model.DoctorId == 0 ? "Create Profile" : "Save Changes";
            return View(model);
        }

        // --- Appointment Management Actions ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string approvedTimeString)
        {
            var result = await _appointmentService.ApproveAppointmentAsync(id, TimeSpan.Parse(approvedTimeString));
            TempData["Message"] = result.Succeeded ? "Appointment approved successfully!" : $"Failed to approve appointment: {result.Errors.FirstOrDefault()?.Description}";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _appointmentService.RejectAppointmentAsync(id);
            TempData["Message"] = result.Succeeded ? "Appointment rejected successfully!" : $"Failed to reject appointment: {result.Errors.FirstOrDefault()?.Description}";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _appointmentService.CancelAppointmentAsync(id);
            TempData["Message"] = result.Succeeded ? "Appointment cancelled successfully!" : $"Failed to cancel appointment: {result.Errors.FirstOrDefault()?.Description}";
            return RedirectToAction(nameof(MyAppointments));
        }
    }
}
