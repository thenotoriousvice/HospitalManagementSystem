using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.ViewModels
{
    public class PatientRegisterViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        [StringLength(10, ErrorMessage = "Gender cannot exceed 10 characters.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Contact Number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(15, ErrorMessage = "Contact Number cannot exceed 15 characters.")]
        public string ContactNumber { get; set; } // Used as UserName/Email for Identity

        [Required(ErrorMessage = "Email Address is required.")] // <<<< ADD THIS
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]  // <<<< ADD THIS
        [StringLength(255, ErrorMessage = "Email Address cannot exceed 255 characters.")] // <<<< ADD THIS
        public string Email { get; set; } // <<<< ADD THIS PROPERTY

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        public string Address { get; set; }

        public string MedicalHistory { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
