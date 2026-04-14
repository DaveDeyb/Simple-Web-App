using System.ComponentModel.DataAnnotations;

namespace SimpleWebApp.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Full name")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
        [RegularExpression(@".*\.com$", ErrorMessage = "Email must end with .com")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9]).{8,128}$", ErrorMessage = "Password must contain at least one letter and one number.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9]).{8,128}$", ErrorMessage = "Password must contain at least one letter and one number.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Register as Administrator")]
        public bool IsAdmin { get; set; } = false;

        [Display(Name = "I agree to the Terms of Service and Privacy Policy")]
        public bool AgreeToTerms { get; set; } = false;
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
        [RegularExpression(@".*\.com$", ErrorMessage = "Email must end with .com")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9]).{8,128}$", ErrorMessage = "Password must contain at least one letter and one number.")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
