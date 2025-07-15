using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Repository.Models
{
    
    public class DoctorRegistrationViewModel
    {
        

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Phone(ErrorMessage = "Invalid Phone Number.")]
        public string PhoneNumber { get; set; }

        public string Qualification { get; set; }

        [Range(0, 50, ErrorMessage = "Experience years must be between 0 and 50.")]
        [Display(Name = "Experience (Years)")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Department")]
        public int DepartmentId { get; set; } 
    }

    // ViewModel for Doctor Profile Editing
    public class EditProfileViewModel
    {
        public int DoctorId { get; set; } 

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; }

       
        [EmailAddress]
        public string? Email { get; set; } 

        [Phone(ErrorMessage = "Invalid Phone Number.")]
        public string PhoneNumber { get; set; }

        public string Qualification { get; set; }

        [Range(0, 50, ErrorMessage = "Experience years must be between 0 and 50.")]
        [Display(Name = "Experience (Years)")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Department")]
        public int DepartmentId { get; set; } // Allow changing department

       
        [DataType(DataType.Time)]
        [Display(Name = "Working Hours Start")]
        public TimeSpan? WorkingHoursStart { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "Working Hours End")]
        public TimeSpan? WorkingHoursEnd { get; set; }
    }

    // ViewModel for the Doctor's Dashboard
    public class DoctorDashboardViewModel
    {
        public Doctor DoctorProfile { get; set; }
        public IEnumerable<Appointment> PendingAppointments { get; set; }
    }
}
