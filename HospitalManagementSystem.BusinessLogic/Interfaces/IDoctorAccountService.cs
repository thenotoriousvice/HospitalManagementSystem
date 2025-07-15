using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagementSystem.BusinessLogic.Interfaces
{
    public interface IDoctorAccountService
    {
        Task<IdentityResult> RegisterDoctorAsync(DoctorRegistrationViewModel model);
        Task<SignInResult> LoginDoctorAsync(string email, string password, bool rememberMe);
        Task SignOutDoctorAsync();
        Task<Doctor?> GetDoctorProfileByUserIdAsync(string userId);
        Task<IdentityUser?> GetIdentityUserByEmailAsync(string email);
        Task<IdentityUser?> GetCurrentUserAsync();
      
        Task<IdentityResult> UpdateDoctorProfileAsync(EditProfileViewModel model, string userId);
        Task<IEnumerable<Department>> GetAllDepartmentsAsync(); 
        Task<bool> CreateDoctorProfileAsync(Doctor doctorProfile);

        Task<IEnumerable<Doctor>> GetAllDoctorsAsync();
    }
}
