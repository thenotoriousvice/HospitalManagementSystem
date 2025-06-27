
using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Data;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.BusinessLogic.Implementation
{
    public class DoctorAccountService : IDoctorAccountService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context; // To manage custom Doctor profiles

        public DoctorAccountService(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context) // Inject DbContext
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IdentityResult> RegisterDoctorAsync(DoctorRegistrationViewModel model)
        {
            // 1. Create IdentityUser
            var user = new IdentityUser { UserName = model.Email, Email = model.Email, PhoneNumber = model.PhoneNumber };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // 2. Assign "Doctor" role
                await _userManager.AddToRoleAsync(user, "Doctor");

                // 3. Create Doctor profile entry in your custom Doctors table
                var doctor = new Doctor
                {
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Qualification = model.Qualification,
                    ExperienceYears = model.ExperienceYears,
                    DepartmentId = model.DepartmentId,
                    IdentityUserId = user.Id, // Link to IdentityUser
                    WorkingHoursStart = null, // Set by doctor later
                    WorkingHoursEnd = null    // Set by doctor later
                };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                // 4. Sign in the new doctor
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            return result;
        }

        public async Task<SignInResult> LoginDoctorAsync(string email, string password, bool rememberMe)
        {
            return await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
        }

        public async Task SignOutDoctorAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<Doctor?> GetDoctorProfileByUserIdAsync(string userId)
        {
            return await _context.Doctors
                                 .Include(d => d.IdentityUser)
                                 .Include(d => d.Department)
                                 .FirstOrDefaultAsync(d => d.IdentityUserId == userId);
        }

        public async Task<IdentityUser?> GetIdentityUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<IdentityUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(_signInManager.Context.User);
        }

        public async Task<IdentityResult> UpdateDoctorProfileAsync(EditProfileViewModel model, string userId)
        {
            // Find the doctor profile based on the logged-in user's ID, which is more reliable.
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Associated user identity not found." });
            }

            // If no doctor profile exists, create a new one.
            if (doctor == null)
            {
                doctor = new Doctor
                {
                    IdentityUserId = userId // Crucially, link it to the current user
                };
                _context.Doctors.Add(doctor);
            }

            // Update Doctor custom profile with data from the view model
            doctor.FullName = model.FullName;
            doctor.PhoneNumber = model.PhoneNumber;
            doctor.Qualification = model.Qualification;
            doctor.ExperienceYears = model.ExperienceYears;
            doctor.DepartmentId = model.DepartmentId;
            doctor.WorkingHoursStart = model.WorkingHoursStart;
            doctor.WorkingHoursEnd = model.WorkingHoursEnd;

            // Use a try-catch block to handle potential database errors
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception if needed and return a failure
                return IdentityResult.Failed(new IdentityError { Description = $"Database error: {ex.Message}" });
            }

            // Separately, update IdentityUser's details if they can be changed.
            // Note: It's good practice to keep UserName and Email in sync.
            user.PhoneNumber = model.PhoneNumber;
            // If you allow email changes, they should be handled with care (e.g., re-verification)
            // user.Email = model.Email;
            // user.UserName = model.Email;

            var updateResult = await _userManager.UpdateAsync(user);

            // The operation is successful only if both the profile save and identity update succeed.
            return updateResult.Succeeded ? IdentityResult.Success : updateResult;
        }

        public async Task<IEnumerable<Department>> GetAllDepartmentsAsync()
        {
            return await _context.Departments.ToListAsync();
        }
    }
}
