using System.ComponentModel.DataAnnotations;

namespace PathForge.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        // Email shown but not editable
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Bio { get; set; }

        // Optional password change — blank means no change
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must be 8+ characters with an uppercase letter, a lowercase letter, a number and a special character")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string? ConfirmNewPassword { get; set; }
    }
}
