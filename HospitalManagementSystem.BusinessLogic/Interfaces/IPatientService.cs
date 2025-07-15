
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagementSystem.BusinessLogic.Interfaces
{
    public interface IPatientService

    {

        
        Task<IdentityResult> CreatePatientAsync(Patient patient, string password);

       

        Task AddPatientProfileForExistingUserAsync(Patient patient); 

       

        Task<Patient> GetPatientByUserIdAsync(string identityUserId);

      

        Task<bool> UpdatePatientAsync(Patient patient);

      

        Task<bool> DeletePatientAndUserAsync(string identityUserId);

       

        Task<IEnumerable<Patient>> GetAllPatients();

    }

}
