using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System; // Added for Exception handling

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AdminController(IPatientService patientService, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _patientService = patientService;
            _userManager = userManager;
            _signInManager = signInManager;
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

                        if (result.Succeeded)
                        {
                            // Check if the user is an Admin
                            if (await _userManager.IsInRoleAsync(user, "Admin"))
                            {
                                if (Url.IsLocalUrl(returnUrl))
                                {
                                    return Redirect(returnUrl);
                                }
                                return RedirectToAction("Dashboard", "Admin"); // Redirect Admin to Admin Dashboard
                            }
                            // Check if the user is a Doctor
                            else if (await _userManager.IsInRoleAsync(user, "Doctor"))
                            {
                                if (Url.IsLocalUrl(returnUrl))
                                {
                                    return Redirect(returnUrl);
                                }
                                // Redirect Doctor to Doctor Dashboard
                                return RedirectToAction("Dashboard", "Doctor");
                            }
                            // Add other roles here if needed (e.g., Patient)
                            else if (await _userManager.IsInRoleAsync(user, "Patient"))
                            {
                                if (Url.IsLocalUrl(returnUrl))
                                {
                                    return Redirect(returnUrl);
                                }
                                // Redirect Patient to Patient Dashboard/Profile (adjust controller/action as needed)
                                return RedirectToAction("MyProfile", "Patient");
                            }
                            else
                            {
                                // If user is logged in but has no specific role for redirection,
                                // you might want to redirect them to a generic home page or sign them out
                                await _signInManager.SignOutAsync();
                                ModelState.AddModelError(string.Empty, "Your account does not have a recognized role to access a specific dashboard. Please contact support.");
                                return View(model);
                            }
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

        public IActionResult Doctors()
        {
            ViewData["Title"] = "Manage Doctors";
            return View();
        }

        public IActionResult Appointments()
        {
            ViewData["Title"] = "Manage Appointments";
            return View();
        }

        public IActionResult RegisterDoctor()
        {
            ViewData["Title"] = "Register New Doctor";
            return View("~/Views/Doctor/Register.cshtml"); // Explicitly return the Doctor Register view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctor(UserRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Doctor");
                    TempData["Message"] = "Doctor registered successfully!";
                    ModelState.Clear();
                    // After successful registration, you might want to redirect to a list of doctors or back to Admin Dashboard
                    return RedirectToAction("Doctors", "Admin"); // Or Dashboard
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            ViewData["Title"] = "Register New Doctor";
            return View("~/Views/Doctor/Register.cshtml", model); // Explicitly return the Doctor Register view with model
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
