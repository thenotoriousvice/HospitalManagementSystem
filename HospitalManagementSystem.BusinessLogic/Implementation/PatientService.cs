
using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Data;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.BusinessLogic.Implementation
{
    public class PatientService : IPatientService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context; // Direct access to DbContext for Patient entity

        // Constructor to inject UserManager and ApplicationDbContext.
        public PatientService(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Creates a new Identity user and a linked Patient profile.
        public async Task<IdentityResult> CreatePatientAsync(Patient patient, string password)
        {
            // Create a new IdentityUser based on patient's contact number (or a dedicated username/email field).
            // Using ContactNumber as UserName/Email for simplicity in this example. Consider a dedicated username/email field for Identity.
            var user = new IdentityUser { UserName = patient.ContactNumber, Email = patient.Email };

            // Attempt to create the Identity user with the provided password.
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // If Identity user creation is successful, link the patient profile to this user.
                patient.IdentityUserId = user.Id;
                _context.Patients.Add(patient); // Add the patient profile to the database
                await _context.SaveChangesAsync(); // Save changes to the database
            }

            return result; // Return the result of Identity user creation
        }

        // Adds a new patient profile for an already existing Identity user.
        // THIS IS THE METHOD THAT THE ERROR IS REFERRING TO. IT MUST BE HERE.
        public async Task AddPatientProfileForExistingUserAsync(Patient patient)
        {
            // Basic validation for required fields.
            if (string.IsNullOrWhiteSpace(patient.IdentityUserId))
            {
                throw new ArgumentException("IdentityUserId is required to link a patient profile to an existing user.");
            }
            if (string.IsNullOrWhiteSpace(patient.Name))
            {
                throw new ArgumentException("Patient name cannot be empty.", nameof(patient.Name));
            }
            // Add more validation as needed (e.g., DateOfBirth, Gender, ContactNumber, Address)

            // Check if a patient profile already exists for this IdentityUser to prevent duplicates.
            var existingPatient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == patient.IdentityUserId);
            if (existingPatient != null)
            {
                throw new InvalidOperationException($"A patient profile already exists for user ID '{patient.IdentityUserId}'.");
            }

            // Add the new patient profile to the database.
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
        }

        // Retrieves a patient profile by their associated Identity User ID.
        public async Task<Patient> GetPatientByUserIdAsync(string identityUserId)
        {
            // Include the IdentityUser navigation property if you need its details (e.g., Email, UserName).
            return await _context.Patients
                .Include(p => p.IdentityUser)
                .FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId);
        }

        // Updates an existing patient profile.
        public async Task<bool> UpdatePatientAsync(Patient patient)
        {
            // Ensure the patient exists before attempting to update.
            var existingPatient = await _context.Patients.FindAsync(patient.PatientId);
            if (existingPatient == null)
            {
                return false; // Patient not found
            }

            // Update the properties of the existing patient entity.
            existingPatient.Name = patient.Name;
            existingPatient.DateOfBirth = patient.DateOfBirth;
            existingPatient.Gender = patient.Gender;
            existingPatient.ContactNumber = patient.ContactNumber;
            existingPatient.Address = patient.Address;
            existingPatient.MedicalHistory = patient.MedicalHistory;

            // Also update the associated IdentityUser's email/username if needed (e.g., if contact number changes).
            var identityUser = await _userManager.FindByIdAsync(existingPatient.IdentityUserId);
            if (identityUser != null)
            {
                // For simplicity, updating UserName and Email to match ContactNumber.
                // In a real app, you might have separate fields for login email/username.
                identityUser.UserName = patient.ContactNumber;
                identityUser.Email = patient.ContactNumber;
                var userUpdateResult = await _userManager.UpdateAsync(identityUser);
                if (!userUpdateResult.Succeeded)
                {
                    // Handle Identity user update failure, e.g., log errors.
                    // For now, we proceed with patient update even if user update fails.
                }
            }

            _context.Patients.Update(existingPatient); // Mark the entity as modified
            await _context.SaveChangesAsync(); // Save changes to the database
            return true;
        }

        // Deletes a patient profile and their associated Identity user account.
        public async Task<bool> DeletePatientAndUserAsync(string identityUserId)
        {
            var patientToDelete = await _context.Patients
                .FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId);

            if (patientToDelete == null)
            {
                return false; // Patient profile not found
            }

            var userToDelete = await _userManager.FindByIdAsync(identityUserId);
            if (userToDelete == null)
            {
                // User not found, but patient profile exists. Decide how to handle this.
                // For now, we'll still delete the patient profile.
                _context.Patients.Remove(patientToDelete);
                await _context.SaveChangesAsync();
                return true;
            }

            // Delete the patient profile first.
            _context.Patients.Remove(patientToDelete);
            await _context.SaveChangesAsync();

            // Then delete the associated Identity user.
            var result = await _userManager.DeleteAsync(userToDelete);
            return result.Succeeded;
        }

        // Retrieves all patients (for admin view, potentially).
        public async Task<IEnumerable<Patient>> GetAllPatients()
        {
            // Include IdentityUser to display username in admin view.
            return await _context.Patients.Include(p => p.IdentityUser).ToListAsync();
        }
    }
}
