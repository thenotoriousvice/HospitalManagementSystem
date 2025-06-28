using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Models;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IAppointmentService _appointmentService;

        public PatientController(IPatientService patientService,
                                 UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager,
                                 IAppointmentService appointmentService)
        {
            _patientService = patientService;
            _userManager = userManager;
            _signInManager = signInManager;
            _appointmentService = appointmentService;
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(PatientRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var patient = new Patient
                {
                    Name = model.Name,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    ContactNumber = model.ContactNumber,
                    Address = model.Address,
                    MedicalHistory = model.MedicalHistory,
                    Email = model.Email,
                    PhoneNumber = model.ContactNumber
                };

                try
                {
                    // Create patient and get the result
                    var result = await _patientService.CreatePatientAsync(patient, model.Password);

                    if (result.Succeeded)
                    {
                        // Try to find the user by email first, then by contact number
                        var newUser = await _userManager.FindByEmailAsync(model.Email) ?? 
                                      await _userManager.FindByNameAsync(model.ContactNumber);

                        if (newUser != null)
                        {
                            // Add the "Patient" role to the new user
                            var roleResult = await _userManager.AddToRoleAsync(newUser, "Patient");
                            if (!roleResult.Succeeded)
                            {
                                // Log the error
                                var errorMessages = string.Join("\n", roleResult.Errors.Select(e => e.Description));
                                Console.WriteLine($"Role assignment failed: {errorMessages}");
                                
                                // Add errors to ModelState
                                foreach (var error in roleResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                return View(model);
                            }

                            // Sign in the user
                            await _signInManager.SignInAsync(newUser, isPersistent: false);
                            TempData["SuccessMessage"] = "Registration successful! You are now logged in.";
                            return RedirectToAction("MyProfile", "Patient");
                        }
                        else
                        {
                            // Log the error
                            Console.WriteLine("User registration succeeded but user not found after creation");
                            ModelState.AddModelError(string.Empty, "Registration succeeded but we couldn't log you in. Please try logging in manually.");
                            return View(model);
                        }
                    }
                    else
                    {
                        // Log the error
                        var errorMessages = string.Join("\n", result.Errors.Select(e => e.Description));
                        Console.WriteLine($"Patient creation failed: {errorMessages}");
                        
                        // Add errors to ModelState
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log any unexpected errors
                    Console.WriteLine($"Unexpected error during registration: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration. Please try again later.");
                }
            }
            
            // Return to view with validation errors
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
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
                var user = await _userManager.FindByNameAsync(model.ContactNumberOrEmail);
                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(model.ContactNumberOrEmail);
                }

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        if (await _userManager.IsInRoleAsync(user, "Patient"))
                        {
                            if (Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }
                            return RedirectToAction("MyProfile", "Patient");
                        }
                        else
                        {
                            // User logged in but is NOT a Patient. Sign them out and show an error.
                            await _signInManager.SignOutAsync();
                            ModelState.AddModelError(string.Empty, "You do not have patient privileges to access this login. Please use the appropriate login page.");
                            return View(model);
                        }
                    }
                    // If login failed (e.g., wrong password or locked out)
                    else
                    {
                        // Sign out any existing user before showing invalid credentials
                        await _signInManager.SignOutAsync();
                        if (result.IsLockedOut)
                        {
                            ModelState.AddModelError(string.Empty, "Account locked out.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your contact number/email and password.");
                        }
                        return View(model);
                    }
                }
                // If user is null (not found)
                // Sign out any existing user before showing invalid credentials
                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your contact number/email and password.");
            }
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

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User ID not found.");
            }

            var patient = await _patientService.GetPatientByUserIdAsync(userId);
            if (patient == null)
            {
                // Handle case where patient profile is not found, perhaps redirect to create profile
                return RedirectToAction("CreateProfile"); // Or a suitable error page
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            // Fetch appointments for the current patient using their patient ID
            var allAppointments = await _appointmentService.GetAppointmentsByPatientIdAsync(patient.PatientId);

            // Filter appointments into upcoming and past
            var upcomingAppointments = allAppointments
                .Where(a => a.AppointmentDate.Date >= DateTime.Today &&
                             (a.Status == AppointmentStatus.Approved || a.Status == AppointmentStatus.Pending))
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToList();

            var pastAppointments = allAppointments
                .Where(a => a.AppointmentDate.Date < DateTime.Today ||
                             a.Status == AppointmentStatus.Cancelled ||
                             a.Status == AppointmentStatus.Rejected ||
                             a.Status == AppointmentStatus.Approved) // Include completed, cancelled, rejected as "past"
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToList();

            var model = new PatientDetailsViewModel
            {
                PatientId = patient.PatientId,
                Name = patient.Name,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                ContactNumber = patient.ContactNumber,
                Address = patient.Address,
                MedicalHistory = patient.MedicalHistory,
                Email = patient.Email,
                PhoneNumber = patient.PhoneNumber,
                UpcomingAppointments = upcomingAppointments,
                PastAppointments = pastAppointments
            };

            return View(model);
        }

        [Authorize(Roles = "Patient")]
        public IActionResult CreateProfile()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(PatientRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

                var existingPatient = await _patientService.GetPatientByUserIdAsync(user.Id);
                if (existingPatient != null)
                {
                    ModelState.AddModelError(string.Empty, "A patient profile already exists for this user.");
                    return View(model);
                }

                var patient = new Patient
                {
                    IdentityUserId = user.Id,
                    Name = model.Name,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    ContactNumber = model.ContactNumber,
                    Address = model.Address,
                    MedicalHistory = model.MedicalHistory
                };

                try
                {
                    await _patientService.AddPatientProfileForExistingUserAsync(patient);
                    return RedirectToAction(nameof(MyProfile));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating your profile.");
                }
            }
            return View(model);
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> UpdateProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            var patient = await _patientService.GetPatientByUserIdAsync(user.Id);
            if (patient == null) return NotFound();

            var model = new PatientUpdateViewModel
            {
                PatientId = patient.PatientId,
                Name = patient.Name,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                ContactNumber = patient.ContactNumber,
                Address = patient.Address,
                MedicalHistory = patient.MedicalHistory
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(PatientUpdateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var patient = await _patientService.GetPatientByUserIdAsync(user.Id);
                if (patient == null || patient.PatientId != model.PatientId) return NotFound();

                patient.Name = model.Name;
                patient.DateOfBirth = model.DateOfBirth;
                patient.Gender = model.Gender;
                patient.ContactNumber = model.ContactNumber;
                patient.Address = model.Address;
                patient.MedicalHistory = model.MedicalHistory;

                var success = await _patientService.UpdatePatientAsync(patient);
                if (success) return RedirectToAction(nameof(MyProfile));

                ModelState.AddModelError(string.Empty, "Error updating profile.");
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProfile(string id)
        {
            Patient patient;
            IdentityUser user;

            if (User.IsInRole("Admin"))
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("Patient ID is required for deletion by Admin.");
                }
                user = await _userManager.FindByIdAsync(id);
                if (user == null) return NotFound($"Unable to find user with ID '{id}'.");
                patient = await _patientService.GetPatientByUserIdAsync(user.Id);
            }
            else
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
                patient = await _patientService.GetPatientByUserIdAsync(user.Id);
            }

            if (patient == null) return NotFound();

            var model = new PatientDetailsViewModel
            {
                PatientId = patient.PatientId,
                Name = patient.Name,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                ContactNumber = patient.ContactNumber,
                Address = patient.Address,
                MedicalHistory = patient.MedicalHistory,
                IdentityUserName = user.UserName
            };
            return View(model);
        }

        [HttpPost, ActionName("DeleteProfile")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Patient ID is required for deletion.");
            }

            var userToDelete = await _userManager.FindByIdAsync(id);
            if (userToDelete == null) return NotFound($"User with ID '{id}' not found.");

            var success = await _patientService.DeletePatientAndUserAsync(userToDelete.Id);
            if (success)
            {
                TempData["SuccessMessage"] = $"Patient profile for {userToDelete.UserName} deleted successfully.";
                return RedirectToAction("Index", "Patient");
            }
            ModelState.AddModelError(string.Empty, "Error deleting patient profile and user account.");
            return RedirectToAction(nameof(DeleteProfile), new { id = id });
        }
    }
}