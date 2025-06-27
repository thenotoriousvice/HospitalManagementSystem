using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Contact Number or Email is required.")]
        [Display(Name = "Contact Number or Email")]
        public string ContactNumberOrEmail { get; set; } // This will act as the username

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
