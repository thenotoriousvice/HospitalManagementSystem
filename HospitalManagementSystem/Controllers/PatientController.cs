using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Models;
using HospitalManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly IPatientService _patientService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IAppointmentService _appointmentService;
        private readonly IBillService _billService;

        public PatientController(IPatientService patientService,
                                 UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager,
                                 IAppointmentService appointmentService,
                                 IBillService billService)
        {
            _patientService = patientService;
            _userManager = userManager;
            _signInManager = signInManager;
            _appointmentService = appointmentService;
            _billService = billService;
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
                // Check if a user with the provided email already exists
                var userByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (userByEmail != null)
                {
                    ModelState.AddModelError("Email", "A user with this email address already exists.");
                    return View(model);
                }

                // Check if a user with the provided contact number  already exists
                var userByContactNumber = await _userManager.FindByNameAsync(model.ContactNumber);
                if (userByContactNumber != null)
                {
                    ModelState.AddModelError("ContactNumber", "A user with this contact number already exists.");
                    return View(model);
                }


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
                                
                                var errorMessages = string.Join("\n", roleResult.Errors.Select(e => e.Description));
                                Console.WriteLine($"Role assignment failed: {errorMessages}");

                                
                                foreach (var error in roleResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                return View(model);
                            }

                            
                            await _signInManager.SignInAsync(newUser, isPersistent: false);
                            TempData["SuccessMessage"] = "Registration successful! You are now logged in.";
                            return RedirectToAction("MyProfile", "Patient");
                        }
                        else
                        {
                            Console.WriteLine("User registration succeeded but user not found after creation");
                            ModelState.AddModelError(string.Empty, "Registration succeeded but we couldn't log you in. Please try logging in manually.");
                            return View(model);
                        }
                    }
                    else
                    {
                        
                        var errorMessages = string.Join("\n", result.Errors.Select(e => e.Description));
                        Console.WriteLine($"Patient creation failed: {errorMessages}");

                        
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                   
                    Console.WriteLine($"Unexpected error during registration: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred during registration. Please try again later.");
                }
            }

            
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
                            // Store the user's identifier in TempData
                            TempData["UserIdentifier"] = model.ContactNumberOrEmail;

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
                    else
                    {
                       
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
                
                return RedirectToAction("CreateProfile"); 
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            // fetch appointments for the current patient using their patient ID
            var allAppointments = await _appointmentService.GetAppointmentsByPatientIdAsync(patient.PatientId);

            
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
                             a.Status == AppointmentStatus.Completed ||
                             a.Status == AppointmentStatus.PaymentCompleted) 
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToList();

            var patientBills = _billService.GetBillsByPatientId(patient.PatientId);

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
                PastAppointments = pastAppointments,
                PatientBills = patientBills
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

        [HttpGet]
        public IActionResult GetBillDetails(int billId)
        {
           
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not logged in."); 
            }

            var patient = _patientService.GetPatientByUserIdAsync(userId).Result; 
            if (patient == null)
            {
                return Unauthorized("Patient profile not found.");
            }

            var bill = _billService.GetBillById(billId);

            if (bill == null)
            {
                return NotFound("Bill not found.");
            }

            
            if (bill.PatientId != patient.PatientId)
            {
                return Forbid("You do not have permission to view this bill.");
            }

            
            return Json(new
            {
                bill.BillId,
                bill.PatientId,
                bill.TotalAmount,
                bill.BillDate,
                bill.Status,
                bill.UploadedFilePath 
            });
        }

        [HttpGet]
        public async Task<IActionResult> ProcessPayment(int billId)
        {
            

            var bill = _billService.GetBillById(billId);

            if (bill == null)
            {
                TempData["ErrorMessage"] = "Bill not found.";
                return RedirectToAction("MyProfile"); 
            }

            if (bill.Status == BillStatus.PAID)
            {
                TempData["InfoMessage"] = "This bill has already been paid.";
                return RedirectToAction("MyProfile");
            }

            // Pass the bill object to the view for payment processing
            return View(bill);
        }


        

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> ProcessPayment(int billId, string paymentMethod)
        {
           

            var bill = _billService.GetBillById(billId);

            if (bill == null)
            {
                return NotFound("Bill not found for payment processing.");
            }

            //if (bill.PatientId != patient.PatientId)
            //{
            //    return Forbid("You do not have permission to pay this bill.");
            //}

            if (bill.Status == BillStatus.PAID)
            {
                return BadRequest("This bill has already been paid.");
            }

          
            if (paymentMethod == "Card" || paymentMethod == "Online") 
            {
                bill.Status = BillStatus.PAID;
                _billService.SaveBill(bill); 

               
                var appointment = await _appointmentService.GetAppointmentByBillIdAsync(bill.BillId); // Assuming GetAppointmentByBillIdAsync exists
                if (appointment != null)
                {
                    await _appointmentService.UpdateAppointmentStatusAsync(appointment.Id, AppointmentStatus.PaymentCompleted);
                }


                TempData["SuccessMessage"] = $"Bill {bill.BillId} paid successfully via {paymentMethod}!";
                return RedirectToAction("MyProfile"); 
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid payment method selected.";
                return View(bill); 
            }
        }
    }
}