
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagementSystem.BusinessLogic.Interfaces
{
    public interface IPatientService

    {

        // Creates a new patient profile and an associated Identity user account.

        Task<IdentityResult> CreatePatientAsync(Patient patient, string password);

        // Adds a new patient profile for an already existing Identity user.

        Task AddPatientProfileForExistingUserAsync(Patient patient); // THIS LINE MUST BE HERE AND MATCH EXACTLY

        // Retrieves a patient profile by their associated Identity User ID.

        Task<Patient> GetPatientByUserIdAsync(string identityUserId);

        // Updates an existing patient profile.

        Task<bool> UpdatePatientAsync(Patient patient);

        // Deletes a patient profile and their associated Identity user account.

        Task<bool> DeletePatientAndUserAsync(string identityUserId);

        // Gets all patients (for admin view, potentially).

        Task<IEnumerable<Patient>> GetAllPatients();

    }

}
