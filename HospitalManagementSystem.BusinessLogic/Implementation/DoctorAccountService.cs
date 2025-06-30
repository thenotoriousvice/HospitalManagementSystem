// HospitalManagementSystem.BusinessLogic.Implementation.DoctorAccountService.cs

using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Data; // Assuming ApplicationDbContext is here
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.BusinessLogic.Implementation
{
    public class DoctorAccountService : IDoctorAccountService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager; // Existing in original file
        private readonly ApplicationDbContext _context; // Added: ApplicationDbContext for database operations

        public DoctorAccountService(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, // Existing in original file
            ApplicationDbContext context) // Inject DbContext
        {
            _userManager = userManager;
            _signInManager = signInManager; // Assigned
            _context = context; // Assigned: Initialize ApplicationDbContext
        }

        // NEW IMPLEMENTATION: Method to create a doctor profile (used by AdminController for newly registered doctors)
        public async Task<bool> CreateDoctorProfileAsync(Doctor doctorProfile)
        {
            try
            {
                // Check if a doctor profile already exists for the given IdentityUserId to prevent duplicates
                var existingDoctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == doctorProfile.IdentityUserId);
                if (existingDoctor != null)
                {
                    Console.WriteLine($"Doctor profile for IdentityUser {doctorProfile.IdentityUserId} already exists. Skipping creation.");
                    return false; // Or handle as an update, depending on desired behavior
                }

                _context.Doctors.Add(doctorProfile);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error creating doctor profile: {ex.Message} - Inner Exception: {ex.InnerException?.Message}");
                return false;
            }
        }

        // UPDATED IMPLEMENTATION: Method for a doctor to register (includes creating IdentityUser and Doctor profile)
        public async Task<IdentityResult> RegisterDoctorAsync(DoctorRegistrationViewModel model)
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Doctor");

                var doctor = new Doctor
                {
                    IdentityUserId = user.Id,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Qualification = model.Qualification,
                    ExperienceYears = model.ExperienceYears,
                    DepartmentId = model.DepartmentId // Ensure this is correctly mapped
                };

                try
                {
                    _context.Doctors.Add(doctor);
                    await _context.SaveChangesAsync();
                    // Original RegisterDoctorAsync in DoctorAccountService.cs also signed in the doctor.
                    // If AdminController is calling this, it might not be desired.
                    // Consider if `_signInManager.SignInAsync(user, isPersistent: false);` is needed here or in AdminController.
                    return IdentityResult.Success;
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Console.WriteLine($"Error saving doctor profile during registration: {ex.Message}");
                    // Attempt to clean up the IdentityUser if doctor profile save fails
                    await _userManager.DeleteAsync(user);
                    return IdentityResult.Failed(new IdentityError { Description = "Failed to save doctor profile details after user creation." });
                }
            }
            return result; // Return IdentityResult from user creation errors
        }

        public async Task<SignInResult> LoginDoctorAsync(string email, string password, bool rememberMe)
        {
            return await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
        }

        public async Task SignOutDoctorAsync()
        {
            await _signInManager.SignOutAsync();
        }

        // NEW/UPDATED IMPLEMENTATION: Method to get doctor profile by Identity User ID
        public async Task<Doctor?> GetDoctorProfileByUserIdAsync(string userId)
        {
            return await _context.Doctors
                                 .Include(d => d.Department) // Include Department to get department name
                                 .Include(d => d.IdentityUser) // Include IdentityUser to get user details if needed
                                 .FirstOrDefaultAsync(d => d.IdentityUserId == userId);
        }

        public async Task<IdentityUser?> GetIdentityUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IdentityUser?> GetCurrentUserAsync()
        {
            // This assumes the HttpContext.User is available, which is usually true in a web context.
            // Ensure this method is called within an authenticated context where _signInManager.Context.User is populated.
            return await _userManager.GetUserAsync(_signInManager.Context.User);
        }


        // NEW/UPDATED IMPLEMENTATION: Method to update doctor profile
        public async Task<IdentityResult> UpdateDoctorProfileAsync(EditProfileViewModel model, string userId)
        {
            var doctor = await _context.Doctors
                                         .FirstOrDefaultAsync(d => d.IdentityUserId == userId);
            // The original file's UpdateDoctorProfileAsync also tried to find the IdentityUser and update its PhoneNumber.
            // If you intend to update IdentityUser properties like PhoneNumber, you'll need to fetch the user:
            // var user = await _userManager.FindByIdAsync(userId);
            // if (user == null) { /* handle error */ }
            // user.PhoneNumber = model.PhoneNumber;
            // var userUpdateResult = await _userManager.UpdateAsync(user);
            // If user update fails, return that result.

            if (doctor == null)
            {
                // If doctor profile does not exist, consider creating it here
                // This handles cases where a user registers but the profile details aren't fully saved immediately
                doctor = new Doctor
                {
                    IdentityUserId = userId,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Qualification = model.Qualification,
                    ExperienceYears = model.ExperienceYears,
                    DepartmentId = model.DepartmentId,
                    WorkingHoursStart = model.WorkingHoursStart,
                    WorkingHoursEnd = model.WorkingHoursEnd
                };
                _context.Doctors.Add(doctor);
            }
            else
            {
                // Update existing properties
                doctor.FullName = model.FullName;
                doctor.PhoneNumber = model.PhoneNumber;
                doctor.Qualification = model.Qualification;
                doctor.ExperienceYears = model.ExperienceYears;
                doctor.DepartmentId = model.DepartmentId;
                doctor.WorkingHoursStart = model.WorkingHoursStart;
                doctor.WorkingHoursEnd = model.WorkingHoursEnd;
                _context.Doctors.Update(doctor);
            }

            try
            {
                await _context.SaveChangesAsync();
                return IdentityResult.Success;
            }
            catch (DbUpdateConcurrencyException)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Concurrency error: Profile was modified by another user. Please try again." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating doctor profile: {ex.Message} - Inner Exception: {ex.InnerException?.Message}");
                return IdentityResult.Failed(new IdentityError { Description = $"An error occurred while saving your profile: {ex.Message}" });
            }
        }

        // NEW IMPLEMENTATION: Method to get all departments (used for dropdowns)
        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
        {
            return await _context.Doctors.Include(d => d.IdentityUser).Include(d => d.Department).ToListAsync();
        }
    }
}